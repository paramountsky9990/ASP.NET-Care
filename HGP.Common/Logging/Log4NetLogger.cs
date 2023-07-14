using System;
using System.Diagnostics;
using log4net;

namespace HGP.Common.Logging
{
    public class Log4NetLogger : ILogger
    {
        private readonly ILog log4NetLogger;

        public Log4NetLogger(Type type)
        {
            this.log4NetLogger = log4net.LogManager.GetLogger(type);
        }
        

        public static ILogger GetLogger()
        {
            var stack = new StackTrace();
            var frame = stack.GetFrame(1);
            return new Log4NetLogger(frame.GetMethod().DeclaringType);
        }

    
        public bool IsEnabled(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return log4NetLogger.IsDebugEnabled;
                case LogLevel.Information:
                    return log4NetLogger.IsInfoEnabled;
                case LogLevel.Warning:
                    return log4NetLogger.IsWarnEnabled;
                case LogLevel.Error:
                    return log4NetLogger.IsErrorEnabled;
                case LogLevel.Fatal:
                    return log4NetLogger.IsFatalEnabled;
            }
            return false;
        }

        public void Log(LogLevel level, Exception exception, string format, params object[] args)
        {
            if (args == null)
            {
                switch (level)
                {
                    case LogLevel.Debug:
                        log4NetLogger.Debug(format, exception);
                        break;
                    case LogLevel.Information:
                        log4NetLogger.Info(format, exception);
                        break;
                    case LogLevel.Warning:
                        log4NetLogger.Warn(format, exception);
                        break;
                    case LogLevel.Error:
                        log4NetLogger.Error(format, exception);
                        break;
                    case LogLevel.Fatal:
                        log4NetLogger.Fatal(format, exception);
                        break;
                }
            }
            else
            {
                switch (level)
                {
                    case LogLevel.Debug:
                        log4NetLogger.DebugFormat(format, args);
                        break;
                    case LogLevel.Information:
                        log4NetLogger.InfoFormat(format, args);
                        break;
                    case LogLevel.Warning:
                        log4NetLogger.WarnFormat(format, args);
                        break;
                    case LogLevel.Error:
                        log4NetLogger.ErrorFormat(format, args);
                        break;
                    case LogLevel.Fatal:
                        log4NetLogger.FatalFormat(format, args);
                        break;
                }
            }
        }
    }
}
