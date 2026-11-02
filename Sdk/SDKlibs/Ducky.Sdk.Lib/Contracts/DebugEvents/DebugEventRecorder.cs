using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ducky.Sdk.Logging;

namespace Ducky.Sdk.Contracts.DebugEvents;

/// <summary>
/// Recorder side of the debug event channel. The core (<see cref="HandleMessage"/>, <see cref="Stop"/>) is plain C#
/// with the hub, the writer and the logs injected, so the decisions are unit-tested without the game. The static
/// <see cref="Start(string,bool)"/> and <see cref="Release"/> are the glue: they open the file, claim the alias and
/// release them again. Main thread only.
/// </summary>
internal sealed class DebugEventRecorder
{
    private static readonly object Gate = new();
    private static DebugEventRecorder? _current;

    private readonly object _stateGate = new();
    private readonly IDebugEventHost _host;
    private readonly DebugEventLineWriter _writer;
    private readonly Action<string> _warn;
    private readonly Action<string> _error;
    private readonly Action? _closeSink;
    private readonly HashSet<string> _warned = new();
    private bool _active = true;

    /// <param name="host">Hub and clock. The core replies and unregisters through it.</param>
    /// <param name="writer">Writes the lines. Created by the glue or by a test.</param>
    /// <param name="warn">Logs one warning line.</param>
    /// <param name="error">Logs one error line.</param>
    /// <param name="closeSink">Closes the underlying file. Called once, by <see cref="Stop"/>.</param>
    internal DebugEventRecorder(
        IDebugEventHost host,
        DebugEventLineWriter writer,
        Action<string> warn,
        Action<string> error,
        Action? closeSink = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _warn = warn ?? throw new ArgumentNullException(nameof(warn));
        _error = error ?? throw new ArgumentNullException(nameof(error));
        _closeSink = closeSink;
    }

    /// <summary>True while this recorder holds the role.</summary>
    internal bool IsActive
    {
        get
        {
            lock (_stateGate)
            {
                return _active;
            }
        }
    }

    /// <summary>
    /// Handles one message sent to the recorder id. Answers hello with ready; writes a batch of any supported version
    /// with or without a prior hello; ignores every other content type with one warning per sender and content type.
    /// Only the documented body fields are read, so a <c>file</c> or <c>path</c> field has no effect.
    /// </summary>
    internal void HandleMessage(string from, string contentType, string body)
    {
        if (!IsActive)
        {
            return;
        }

        if (contentType == DebugEventProtocol.HelloContentType)
        {
            HandleHello(from, body);
            return;
        }

        if (DebugEventProtocol.TryParseBatchVersion(contentType, out var version) &&
            DebugEventProtocol.SupportedVersions.Contains(version))
        {
            HandleBatch(from, contentType, body);
            return;
        }

        WarnOnce(from, contentType, "unsupported content type");
    }

    private void HandleHello(string from, string body)
    {
        if (!DebugEventProtocol.TryDecodeHello(body, out _, out var versions))
        {
            WarnOnce(from, DebugEventProtocol.HelloContentType, "malformed body");
            return;
        }

        var version = DebugEventProtocol.PickVersion(versions, DebugEventProtocol.SupportedVersions);
        _host.Send(
            DebugEventProtocol.RecorderModId,
            from,
            DebugEventProtocol.ReadyContentType,
            DebugEventProtocol.EncodeReady(version, DebugEventProtocol.SupportedVersions));
    }

    private void HandleBatch(string from, string contentType, string body)
    {
        if (!DebugEventProtocol.TryDecodeBatch(body, out var batch))
        {
            WarnOnce(from, contentType, "malformed body");
            return;
        }

        try
        {
            _writer.Write(from, batch);
            _writer.Flush();
        }
        catch (Exception e)
        {
            _error($"Debug event recorder stopped: writing failed: {e.Message}");
            Stop();
            return;
        }

        if (_writer.CapReached)
        {
            _warn("Debug event recorder stopped: the file reached 64 MiB.");
            Stop();
        }
    }

    private void WarnOnce(string from, string contentType, string reason)
    {
        if (_warned.Add(from + "\n" + contentType))
        {
            _warn($"Debug event recorder ignored a {contentType} message from {from}: {reason}.");
        }
    }

    /// <summary>
    /// Flushes and closes the file, unregisters the alias and drops the role. Safe to call more than once; the second
    /// call does nothing.
    /// </summary>
    internal void Stop()
    {
        lock (_stateGate)
        {
            if (!_active)
            {
                return;
            }

            _active = false;
        }

        try
        {
            _writer.Flush();
        }
        catch (Exception e)
        {
            _error($"Debug event recorder could not flush its file: {e.Message}");
        }

        try
        {
            _closeSink?.Invoke();
        }
        catch (Exception e)
        {
            _error($"Debug event recorder could not close its file: {e.Message}");
        }

        try
        {
            _host.Unregister(DebugEventProtocol.RecorderModId);
        }
        catch (Exception e)
        {
            _error($"Debug event recorder could not unregister: {e.Message}");
        }

        lock (Gate)
        {
            if (ReferenceEquals(_current, this))
            {
                _current = null;
            }
        }
    }

    #region Per-mod role (glue)

    /// <summary>True while this mod copy holds the recorder role.</summary>
    internal static bool IsRecorderActive
    {
        get
        {
            lock (Gate)
            {
                return _current != null && _current.IsActive;
            }
        }
    }

    /// <summary>Starts the recorder for this mod with the real hub. Main thread only.</summary>
    internal static bool Start(string filePath, bool append)
    {
        return Start(
            filePath,
            append,
            new DebugEventReporter.ModHttpV1DebugEventHost(),
            message => Log.Warn(message),
            message => Log.Error(message));
    }

    /// <summary>
    /// Claims the recorder role. Refuses, with one warning and without touching the file, when a recorder is already
    /// running in this mod or when the alias is registered by anyone. Otherwise opens the file (UTF-8 without BOM,
    /// truncated or appended, read-shared), registers the alias and confirms the registration by re-reading the id
    /// list. Returns false on any refusal or failure.
    /// </summary>
    /// <remarks>
    /// The hub exposes only the list of ids, not which callback owns an id, so the confirmation checks the id is
    /// present; it cannot tell this callback from another one registered under the same id in the same instant
    /// (design D4).
    /// </remarks>
    internal static bool Start(
        string filePath,
        bool append,
        IDebugEventHost host,
        Action<string> warn,
        Action<string> error)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            error("StartRecorder needs a file path; no recorder was started.");
            return false;
        }

        if (IsRecorderActive)
        {
            warn("StartRecorder refused: this mod's debug event recorder is already running.");
            return false;
        }

        if (host.GetModIds().Contains(DebugEventProtocol.RecorderModId))
        {
            warn("StartRecorder refused: another debug event recorder is already registered; the existing one keeps running.");
            return false;
        }

        FileStream? stream = null;
        StreamWriter? sink = null;
        long existingLines = 0;
        long existingBytes = 0;
        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var needsNewline = false;
            if (append && File.Exists(fullPath))
            {
                (existingLines, existingBytes, var endsWithNewline) = MeasureFile(fullPath);
                needsNewline = existingBytes > 0 && !endsWithNewline;
            }

            stream = new FileStream(
                fullPath,
                append ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.Read);
            sink = new StreamWriter(stream, new UTF8Encoding(false));
            if (needsNewline)
            {
                // A crash left a partial last line; start on a new line so every new line stays parseable.
                sink.Write('\n');
                sink.Flush();
                existingBytes += 1;
            }
        }
        catch (Exception e)
        {
            error($"StartRecorder could not open '{filePath}': {e.Message}");
            sink?.Dispose();
            stream?.Dispose();
            return false;
        }

        var writer = new DebugEventLineWriter(sink, () => host.SecondsSinceStart, existingLines, existingBytes);
        var recorderFile = sink;
        var recorder = new DebugEventRecorder(host, writer, warn, error, () => recorderFile.Dispose());

        try
        {
            host.Register(DebugEventProtocol.RecorderModId, recorder.HandleMessage);
        }
        catch (Exception e)
        {
            error($"StartRecorder could not register the recorder: {e.Message}");
            recorder.Stop();
            return false;
        }

        if (!host.GetModIds().Contains(DebugEventProtocol.RecorderModId))
        {
            error("StartRecorder could not confirm its registration; no recorder was started.");
            recorder.Stop();
            return false;
        }

        lock (Gate)
        {
            _current = recorder;
        }

        return true;
    }

    /// <summary>Stops this mod's recorder, if it holds the role. Idempotent.</summary>
    internal static void Release()
    {
        DebugEventRecorder? recorder;
        lock (Gate)
        {
            recorder = _current;
        }

        recorder?.Stop();
    }

    /// <summary>Line count, byte count and whether the file ends with a line break, read in chunks.</summary>
    private static (long Lines, long Bytes, bool EndsWithNewline) MeasureFile(string path)
    {
        long lines = 0;
        long bytes = 0;
        var endsWithNewline = true;
        var buffer = new byte[64 * 1024];
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        int read;
        while ((read = file.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                if (buffer[i] == (byte)'\n')
                {
                    lines++;
                }
            }

            bytes += read;
            endsWithNewline = buffer[read - 1] == (byte)'\n';
        }

        return (lines, bytes, endsWithNewline);
    }

    #endregion
}
