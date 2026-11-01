using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Ducky.HotReload
{
    /// <summary>
    /// Loader shim. The game loads this assembly once (Assembly.LoadFrom) and creates this component.
    /// The real mod lives in Ducky.HotReload.Core*.dll, loaded here from bytes into a child GameObject.
    /// With the marker file <c>hotreload.enabled</c> next to the dll, a changed Core dll is swapped in
    /// without restarting the game: destroy Core object (OnDisable cleans up) -> load new bytes -> AddComponent.
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private const string CorePrefix = "Ducky.HotReload.Core";
        private const string CoreTypeName = "Ducky.HotReload.Core.CoreBehaviour";
        private const string MarkerFile = "hotreload.enabled";
        private const float PollSeconds = 0.5f;

        private readonly List<Assembly> _loadedCores = new List<Assembly>();
        private string _dir = "";
        private bool _watch;
        private bool _busy;
        private int _loadCount;
        private GameObject? _coreGo;
        private string? _loadedFile;
        private DateTime _loadedWrite;
        private string? _pendingFile;
        private DateTime _pendingWrite;
        private float _nextPoll;
        private float _nextTick;

        protected override void OnAfterSetup()
        {
            _dir = info.path;
            _watch = File.Exists(Path.Combine(_dir, MarkerFile));
            AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
            Debug.Log($"[HotReload] shim up, dir={_dir}, watch={_watch}");
            StartCoroutine(Reload("initial"));
        }

        private void OnDestroy()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnResolve;
            if (_coreGo != null) Destroy(_coreGo);
            Debug.Log("[HotReload] shim destroyed");
        }

        private void Update()
        {
            if (_busy || _loadCount == 0) return;

            if (Time.unscaledTime >= _nextTick)
            {
                _nextTick = Time.unscaledTime + 1f;
                Probe.RaiseTick();
            }

            if (!_watch || Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + PollSeconds;

            var file = FindLatestCoreFile();
            if (file == null) return;
            var write = File.GetLastWriteTimeUtc(file);
            if (file == _loadedFile && write == _loadedWrite) return;

            // Wait until the file stopped changing (a copy in progress changes mtime/size between polls).
            if (file == _pendingFile && write == _pendingWrite)
            {
                _pendingFile = null;
                StartCoroutine(Reload("file changed"));
            }
            else
            {
                _pendingFile = file;
                _pendingWrite = write;
            }
        }

        private IEnumerator Reload(string reason)
        {
            _busy = true;
            Debug.Log($"[HotReload] reload #{_loadCount + 1} ({reason})");

            if (_coreGo != null)
            {
                var go = _coreGo;
                _coreGo = null;
                try { go.GetComponent<Duckov.Modding.ModBehaviour>()?.NotifyBeforeDeactivate(); }
                catch (Exception e) { Debug.LogException(e); }
                Destroy(go);
                // Unity runs OnDisable/OnDestroy at the end of the frame, not inside Destroy().
                yield return null;
                yield return null;
                Report("after-unload");
                if (File.Exists(Path.Combine(_dir, "probe-remove.enabled")))
                {
                    Debug.Log("[HotReload] probe removal: " + GameStateProbe.RemoveAll());
                    yield return null;
                    Report("after-removal");
                }
            }

            var file = FindLatestCoreFile();
            if (file == null)
            {
                Debug.LogError($"[HotReload] no {CorePrefix}*.dll in {_dir}");
                _busy = false;
                yield break;
            }

            var error = LoadCore(file);
            if (error != null) Debug.LogError($"[HotReload] load failed: {error}");
            yield return null;
            Report(error == null ? "after-load" : "load-failed");
            _busy = false;
        }

        private string? LoadCore(string file)
        {
            try
            {
                var asm = Assembly.Load(File.ReadAllBytes(file));
                var sameAsBefore = _loadedCores.Contains(asm);
                _loadedCores.Add(asm);
                Debug.Log($"[HotReload] loaded {Path.GetFileName(file)} as '{asm.GetName().FullName}' sameAssemblyObjectAsEarlierLoad={sameAsBefore}");

                var type = asm.GetType(CoreTypeName, true)!;
                var go = new GameObject("HotReloadCore");
                go.transform.SetParent(transform, false);
                _coreGo = go;
                var core = (Duckov.Modding.ModBehaviour)go.AddComponent(type); // OnEnable runs here
                core.Setup(master, info);

                _loadedFile = file;
                _loadedWrite = File.GetLastWriteTimeUtc(file);
                _loadCount++;
                return null;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return e.Message;
            }
        }

        /// <summary>One line the test script greps: what is alive after this stage.</summary>
        private void Report(string stage)
        {
            var coreAssemblies = AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name.StartsWith(CorePrefix, StringComparison.Ordinal));
            var coreRunners = FindObjectsOfType<MonoBehaviour>().Count(m => m != null && m.GetType().Name == "CoreRunner");
            Debug.Log($"[HotReload] REPORT load={_loadCount} stage={stage} marker={Probe.ActiveMarker} pingHits={Probe.PingHits()} tickHandlers={Probe.TickHandlerCount} coreRunners={coreRunners} coreAssembliesInDomain={coreAssemblies} {GameStateProbe.Describe()}");
        }

        private string? FindLatestCoreFile() =>
            Directory.GetFiles(_dir, CorePrefix + "*.dll")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

        /// <summary>
        /// Assembly.Load(byte[]) has no location to probe, so references of Core must be answered here:
        /// the shim itself (already loaded through LoadFrom) and stable dependencies next to it (0Harmony).
        /// </summary>
        private Assembly? OnResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            var self = typeof(ModBehaviour).Assembly;
            if (name == self.GetName().Name) return self;
            if (name.StartsWith(CorePrefix, StringComparison.Ordinal)) return null;
            var path = Path.Combine(_dir, name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        }
    }
}
