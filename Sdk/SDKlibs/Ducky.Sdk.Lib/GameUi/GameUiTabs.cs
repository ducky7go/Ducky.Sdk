using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Duckov.UI;
using Ducky.Sdk.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ducky.Sdk.GameUi;

/// <summary>
/// Adds a mod panel (a <see cref="GameUiTabView"/>) and its button to the game's Tab bar, and keeps it there.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why it keeps checking.</b> The tab bar (<c>ViewTabs</c>) lives under the per-level <c>LevelManager(Clone)</c>, so the
/// game builds a new one with every scene. Each registration runs a one-second main-thread check that installs the tab
/// whenever a bar without it exists, and does nothing otherwise.
/// </para>
/// <para>
/// <b>Lifetime.</b> Register in <c>ModEnabled()</c> and hand the result to <c>Subscriptions.Add(...)</c>; disposing it stops the
/// check and destroys the view and the button, so a disabled mod or a hot reload leaves nothing behind.
/// </para>
/// <para>
/// <b>Coupled to the game's layout.</b> The names it looks up (<c>ViewArea</c>, <c>ViewButtons</c> and the template button)
/// were read from the running game (see docs/game-ui.md). If the game renames them the install is skipped and one warning is
/// written; nothing throws.
/// </para>
/// </remarks>
public static class GameUiTabs
{
    private const string ViewAreaName = "ViewArea";
    private const string ViewButtonsName = "ViewButtons";
    private const string ViewTypeNameField = "viewTypeName";

    /// <summary>Starts keeping <typeparamref name="TView"/> and its tab in the game's tab bar. Call on the main thread.</summary>
    public static GameUiTabRegistration Register<TView>(GameUiTabOptions? options = null) where TView : GameUiTabView
    {
        return new GameUiTabRegistration(typeof(TView), options ?? new GameUiTabOptions());
    }

    /// <summary>
    /// Index the new button should take among <paramref name="existingNames"/>: after <paramref name="insertAfter"/>, or last when
    /// it is null or not found.
    /// </summary>
    internal static int ResolveInsertIndex(IReadOnlyList<string> existingNames, string? insertAfter)
    {
        if (insertAfter == null)
        {
            return existingNames.Count;
        }

        for (var i = 0; i < existingNames.Count; i++)
        {
            if (existingNames[i] == insertAfter)
            {
                return i + 1;
            }
        }

        return existingNames.Count;
    }

    internal sealed class Installer
    {
        private readonly Type _viewType;
        private readonly GameUiTabOptions _options;
        private readonly string _buttonName;
        private bool _warnedMissing;
        private bool _warnedConflict;

        internal Installer(Type viewType, GameUiTabOptions options)
        {
            _viewType = viewType;
            _options = options;
            _buttonName = "DuckySdkTab_" + viewType.Name;
        }

        internal GameUiTabView? View { get; private set; }

        /// <summary>Installs the view and button into the current tab bar when they are missing. Safe to call repeatedly.</summary>
        internal void TryInstall(Action open)
        {
            var tabs = Resources.FindObjectsOfTypeAll<ViewTabs>().FirstOrDefault(t => t.gameObject.scene.IsValid());
            if (tabs == null)
            {
                return;
            }

            var viewArea = tabs.transform.Find(ViewAreaName);
            var buttons = tabs.transform.Find(ViewButtonsName);
            var template = buttons == null ? null : buttons.Find(_options.TemplateButtonName);
            if (viewArea == null || buttons == null || template == null)
            {
                if (!_warnedMissing)
                {
                    _warnedMissing = true;
                    Log.Warn("[GameUiTabs] tab bar layout not as expected (need {0}, {1}, {2}); {3} is not installed",
                        ViewAreaName, ViewButtonsName, _options.TemplateButtonName, _viewType.Name);
                }

                return;
            }

            if (View == null)
            {
                if (HasConflict(viewArea))
                {
                    if (!_warnedConflict)
                    {
                        _warnedConflict = true;
                        Log.Warn("[GameUiTabs] a view named {0} already exists; rename the class. Not installed.", _viewType.Name);
                    }

                    return;
                }

                View = CreateView(viewArea);
            }

            if (buttons.Find(_buttonName) == null)
            {
                CreateButton(buttons, template, open);
            }
        }

        internal void Uninstall()
        {
            foreach (var tabs in Resources.FindObjectsOfTypeAll<ViewTabs>())
            {
                if (!tabs.gameObject.scene.IsValid())
                {
                    continue;
                }

                var button = tabs.transform.Find(ViewButtonsName + "/" + _buttonName);
                if (button != null)
                {
                    Object.Destroy(button.gameObject);
                }
            }

            if (View != null)
            {
                Object.Destroy(View.gameObject);
            }

            View = null;
        }

        private bool HasConflict(Transform viewArea)
        {
            foreach (var view in viewArea.GetComponentsInChildren<Duckov.UI.View>(true))
            {
                if (view.GetType().Name == _viewType.Name)
                {
                    return true;
                }
            }

            return false;
        }

        private GameUiTabView CreateView(Transform viewArea)
        {
            // Created inactive so that Awake runs only after the parent is set: View.Awake reads transform.parent.parent as the
            // ViewTabs. Like every game view it then closes itself.
            var go = new GameObject(_viewType.Name, typeof(RectTransform));
            go.SetActive(false);
            go.transform.SetParent(viewArea, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var view = (GameUiTabView)go.AddComponent(_viewType);
            go.SetActive(true);
            return view;
        }

        private void CreateButton(Transform buttons, Transform template, Action open)
        {
            var clone = Object.Instantiate(template.gameObject, buttons);
            clone.name = _buttonName;

            var names = new List<string>();
            foreach (Transform child in buttons)
            {
                if (child.gameObject != clone)
                {
                    names.Add(child.name);
                }
            }

            clone.transform.SetSiblingIndex(ResolveInsertIndex(names, _options.InsertAfter));

            // ViewTabDisplayEntry lights the tab whose viewTypeName equals the opened view's type name.
            var entry = clone.GetComponent<ViewTabDisplayEntry>();
            if (entry != null)
            {
                typeof(ViewTabDisplayEntry).GetField(ViewTypeNameField, BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(entry, _viewType.Name);
            }

            // The template's click is a persistent listener (ViewsProxy.ShowXxx). RemoveAllListeners leaves those, so switch them off.
            var button = clone.GetComponent<GenericButton>();
            if (button != null)
            {
                for (var i = 0; i < button.onPointerClick.GetPersistentEventCount(); i++)
                {
                    button.onPointerClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                }

                button.onPointerClick.AddListener(() => open());
            }

            var icon = clone.transform.Find("Icon");
            if (icon != null && icon.TryGetComponent<Image>(out var image))
            {
                if (_options.Icon != null)
                {
                    image.sprite = _options.Icon;
                }

                if (_options.Tint.HasValue)
                {
                    image.color = _options.Tint.Value;
                }
            }
        }
    }
}

/// <summary>Handle for a tab added with <see cref="GameUiTabs.Register{TView}"/>. Dispose it to remove the tab.</summary>
public sealed class GameUiTabRegistration : IDisposable
{
    private readonly GameUiTabs.Installer _installer;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    internal GameUiTabRegistration(Type viewType, GameUiTabOptions options)
    {
        _installer = new GameUiTabs.Installer(viewType, options);
        RunAsync(_cts.Token).Forget();
    }

    /// <summary>The live view in the current tab bar, or null while there is none (title screen, scene loading).</summary>
    public GameUiTabView? View => _installer.View;

    /// <summary>Opens the view, as a click on its tab does. Does nothing while the view is not installed.</summary>
    public void Open()
    {
        var view = _installer.View;
        if (view != null)
        {
            view.Open();
        }
    }

    /// <summary>Stops the check and destroys the view and the button.</summary>
    public void Dispose()
    {
        if (_cts.IsCancellationRequested)
        {
            return;
        }

        _cts.Cancel();
        _cts.Dispose();
        try
        {
            _installer.Uninstall();
        }
        catch (Exception e)
        {
            Log.Warn(e, "[GameUiTabs] uninstall failed");
        }
    }

    private async UniTaskVoid RunAsync(CancellationToken token)
    {
        // One frame first: after a hot reload the previous registration's objects are destroyed at the end of the frame.
        await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
        while (!token.IsCancellationRequested)
        {
            try
            {
                _installer.TryInstall(Open);
            }
            catch (Exception e)
            {
                Log.Warn(e, "[GameUiTabs] install failed");
            }

            var cancelled = await UniTask.Delay(1000, DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, token)
                .SuppressCancellationThrow();
            if (cancelled)
            {
                return;
            }
        }
    }
}
