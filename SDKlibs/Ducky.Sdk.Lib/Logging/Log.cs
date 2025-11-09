using System;

namespace Ducky.Sdk.Logging;

public static class Log
{
    public static ILog Current { get; set; } = null!;

    #region Level Check Methods

    public static bool IsTraceEnabled() => Current.IsTraceEnabled();
    public static bool IsDebugEnabled() => Current.IsDebugEnabled();
    public static bool IsInfoEnabled() => Current.IsInfoEnabled();
    public static bool IsWarnEnabled() => Current.IsWarnEnabled();
    public static bool IsErrorEnabled() => Current.IsErrorEnabled();
    public static bool IsFatalEnabled() => Current.IsFatalEnabled();

    #endregion

    #region Trace Methods

    public static void Trace(Func<string> messageFunc) => Current.Trace(messageFunc);
    public static void Trace(string message) => Current.Trace(message);
    public static void Trace(string message, params object[] args) => Current.Trace(message, args);

    public static void Trace(Exception exception, string message, params object[] args) =>
        Current.Trace(exception, message, args);

    public static void TraceFormat(string message, params object[] args) => Current.TraceFormat(message, args);

    public static void TraceException(string message, Exception exception) =>
        Current.TraceException(message, exception);

    public static void TraceException(string message, Exception exception, params object[] args) =>
        Current.TraceException(message, exception, args);

    #endregion

    #region Debug Methods

    public static void Debug(Func<string> messageFunc) => Current.Debug(messageFunc);
    public static void Debug(string message) => Current.Debug(message);
    public static void Debug(string message, params object[] args) => Current.Debug(message, args);

    public static void Debug(Exception exception, string message, params object[] args) =>
        Current.Debug(exception, message, args);

    public static void DebugFormat(string message, params object[] args) => Current.DebugFormat(message, args);

    public static void DebugException(string message, Exception exception) =>
        Current.DebugException(message, exception);

    public static void DebugException(string message, Exception exception, params object[] args) =>
        Current.DebugException(message, exception, args);

    #endregion

    #region Info Methods

    public static void Info(Func<string> messageFunc) => Current.Info(messageFunc);
    public static void Info(string message) => Current.Info(message);
    public static void Info(string message, params object[] args) => Current.Info(message, args);

    public static void Info(Exception exception, string message, params object[] args) =>
        Current.Info(exception, message, args);

    public static void InfoFormat(string message, params object[] args) => Current.InfoFormat(message, args);
    public static void InfoException(string message, Exception exception) => Current.InfoException(message, exception);

    public static void InfoException(string message, Exception exception, params object[] args) =>
        Current.InfoException(message, exception, args);

    #endregion

    #region Warn Methods

    public static void Warn(Func<string> messageFunc) => Current.Warn(messageFunc);
    public static void Warn(string message) => Current.Warn(message);
    public static void Warn(string message, params object[] args) => Current.Warn(message, args);

    public static void Warn(Exception exception, string message, params object[] args) =>
        Current.Warn(exception, message, args);

    public static void WarnFormat(string message, params object[] args) => Current.WarnFormat(message, args);
    public static void WarnException(string message, Exception exception) => Current.WarnException(message, exception);

    public static void WarnException(string message, Exception exception, params object[] args) =>
        Current.WarnException(message, exception, args);

    #endregion

    #region Error Methods

    public static void Error(Func<string> messageFunc) => Current.Error(messageFunc);
    public static void Error(string message) => Current.Error(message);
    public static void Error(string message, params object[] args) => Current.Error(message, args);

    public static void Error(Exception exception, string message, params object[] args) =>
        Current.Error(exception, message, args);

    public static void ErrorFormat(string message, params object[] args) => Current.ErrorFormat(message, args);

    public static void ErrorException(string message, Exception exception) =>
        Current.ErrorException(message, exception);

    public static void ErrorException(string message, Exception exception, params object[] args) =>
        Current.ErrorException(message, exception, args);

    #endregion

    #region Fatal Methods

    public static void Fatal(Func<string> messageFunc) => Current.Fatal(messageFunc);
    public static void Fatal(string message) => Current.Fatal(message);
    public static void Fatal(string message, params object[] args) => Current.Fatal(message, args);

    public static void Fatal(Exception exception, string message, params object[] args) =>
        Current.Fatal(exception, message, args);

    public static void FatalFormat(string message, params object[] args) => Current.FatalFormat(message, args);

    public static void FatalException(string message, Exception exception) =>
        Current.FatalException(message, exception);

    public static void FatalException(string message, Exception exception, params object[] args) =>
        Current.FatalException(message, exception, args);

    #endregion
}
