using System;
using Ducky.Sdk.Logging;
using UnityEngine;

namespace DebugEventSamples.Recorder;

/// <summary>
/// The SDK's own log lines (for example the refused StartRecorder warning) go to Log.Current, which is a no-op in the
/// game unless a provider is configured. This forwards them to Unity's log so they reach Player.log.
/// </summary>
internal sealed class SampleUnityLog : ILog
{
    public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception? exception = null,
        params object[] formatParameters)
    {
        if (messageFunc == null)
        {
            return logLevel >= LogLevel.Info;
        }

        var text = "[Ducky.Sdk " + logLevel + "] " + messageFunc();
        if (exception != null)
        {
            text += " " + exception;
        }

        switch (logLevel)
        {
            case LogLevel.Warn:
                Debug.LogWarning(text);
                return true;
            case LogLevel.Error:
            case LogLevel.Fatal:
                Debug.LogError(text);
                return true;
            case LogLevel.Info:
                Debug.Log(text);
                return true;
            default:
                return false;
        }
    }
}
