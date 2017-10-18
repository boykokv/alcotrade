using System;
using System.Diagnostics;
using System.Linq;
using System.Web;
using NLog;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace BB.Core.Log
{
    public static class Logger
    {
        private static NLog.Logger _logger;
        private static NLog.Logger _systemEventLogger;
        private static NLog.Logger _mailLogger;

        static Logger()
        {
            ConfigureSourceParamForEventLogTargets();

            _logger = LogManager.GetLogger("GeneralLogger");
            _mailLogger = LogManager.GetLogger("EmailLogger");
            _systemEventLogger = LogManager.GetLogger("EventLog");

        }

        public static string CurrentApplicationName
        {
            get { return Process.GetCurrentProcess().ProcessName; }
        }

        private static void ConfigureSourceParamForEventLogTargets()
        {
            var unwrappedEventLogTargets = LogManager.Configuration.AllTargets
                .OfType<AsyncTargetWrapper>()
                .Where(wrapper => wrapper.WrappedTarget is EventLogTarget)
                .Select(wrapper => (EventLogTarget)wrapper.WrappedTarget);

            var eventLogTargets = LogManager.Configuration.AllTargets.OfType<EventLogTarget>();

            eventLogTargets.Union(unwrappedEventLogTargets).ToList().ForEach(target => target.Source = CurrentApplicationName);
        }

        public static void TurnOnConsoleOutput()
        {
            _logger = LogManager.GetLogger("GeneralLoggerWithConsoleOutput");
        }

        public static void LogMailError(string message, params object[] values)
        {
            _mailLogger.Log(LogLevel.Error, message, values);
        }

        public static void LogMailInfo(string message, params object[] values)
        {
            _mailLogger.Log(LogLevel.Info, message, values);
        }

        public static void Log(Exception e)
        {
            InnerLog(e, null, LogLevel.Fatal);
        }

        public static void Log(Exception e, string errorMessage)
        {
            InnerLog(e, errorMessage ?? e.Message, LogLevel.Fatal);
        }

        public static void Error(string errorMessage)
        {
            InnerLog(null, errorMessage, LogLevel.Error);
        }

        public static void Error(string errorMessage, Exception e)
        {
            InnerLog(e, errorMessage ?? e.Message, LogLevel.Error);
        }

        public static void Warning(string errorMessage, Exception e = null)
        {
            InnerLog(e, errorMessage, LogLevel.Warn);
        }

        public static void Info(string message, params object[] values)
        {
            InnerLog(null, String.Format(message, values), LogLevel.Info);
        }

        public static void Trace(string message, params object[] values)
        {
            InnerLog(null, String.Format(message, values), LogLevel.Trace);
        }

        public static void Info(string message)
        {
            InnerLog(null, message, LogLevel.Info);
        }

        public static void Flush()
        {
            try
            {
                LogManager.Flush();
            }
            catch
            {
                //логгер ни когда не должен падать
            }
        }

        private static void InnerLog(Exception exception, string errorMessage, LogLevel logLevel)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(errorMessage) && exception != null)
                    errorMessage = exception.Message;

                var logEvent = new LogEventInfo(logLevel, _logger.Name, errorMessage);

                logEvent.Properties["errorMessage"] = errorMessage;

                if (exception != null)
                {
                    logEvent.Properties["fullExceptionData"] = exception.ToString();
                    logEvent.Exception = exception;
                }

                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                    logEvent.Properties["UserHostAddress"] = "From: " + HttpContext.Current.Request.UserHostAddress;

                _logger.Log(logEvent);
            }
            catch
            {
                //логгер ни когда не должен падать
            }
        }

        public static void EventLogInfo(string message, Int32 eventCode, bool logMessageToConsole = true)
        {
            InnerEventLog(LogLevel.Info, message, eventCode, SystemEventLevel.Info, logMessageToConsole);
        }

        public static void EventLogError(string message, Int32 eventCode, bool logMessageToConsole = true)
        {
            InnerEventLog(LogLevel.Error, message, eventCode, SystemEventLevel.Error, logMessageToConsole);
        }

        public static void EventLogWarning(string message, Int32 eventCode, bool logMessageToConsole = true)
        {
            InnerEventLog(LogLevel.Warn, message, eventCode, SystemEventLevel.Warning, logMessageToConsole);
        }

        public static void EventLogCritical(string message, Int32 eventCode, bool logMessageToConsole = true)
        {
            InnerEventLog(LogLevel.Fatal, message, eventCode, SystemEventLevel.Critical, logMessageToConsole);
        }

        private static void InnerEventLog(LogLevel logLevel, string message, int eventCode, SystemEventLevel eventLevel, bool logMessageToConsole)
        {
            try
            {
                var logEvent = new LogEventInfo(logLevel, _systemEventLogger.Name, message);

                logEvent.Properties["eventCode"] = eventCode;
                logEvent.Properties["eventLevel"] = (int)eventLevel;

                _systemEventLogger.Log(logEvent);

                if (logMessageToConsole)
                    Info(string.Format("For EventLog: ({0}) {1}", logLevel, message));
            }
            catch
            {
                //логгер ни когда не должен падать
            }
        }

        internal enum SystemEventLevel
        {
            Info,
            Debug,
            Warning,
            Error,
            Critical
        }
    }
}