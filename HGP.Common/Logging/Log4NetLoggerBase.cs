using System;
using System.Globalization;
using log4net.Core;
using log4net.Util;

namespace HGP.Common.Logging
{
    public interface ILoggerBase
    {
        bool IsDebugEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsFatalEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }
        string ToString();
        void Debug(String message);
        void Debug(Func<string> messageFactory);
        void Debug(String message, Exception exception);
        void DebugFormat(String format, params Object[] args);
        void DebugFormat(Exception exception, String format, params Object[] args);
        void DebugFormat(IFormatProvider formatProvider, String format, params Object[] args);
        void DebugFormat(Exception exception, IFormatProvider formatProvider, String format, params Object[] args);
        void Error(String message);
        void Error(Func<string> messageFactory);
        void Error(String message, Exception exception);
        void ErrorFormat(String format, params Object[] args);
        void ErrorFormat(Exception exception, String format, params Object[] args);
        void ErrorFormat(IFormatProvider formatProvider, String format, params Object[] args);
        void ErrorFormat(Exception exception, IFormatProvider formatProvider, String format, params Object[] args);
        void Fatal(String message);
        void Fatal(Func<string> messageFactory);
        void Fatal(String message, Exception exception);
        void FatalFormat(String format, params Object[] args);
        void FatalFormat(Exception exception, String format, params Object[] args);
        void FatalFormat(IFormatProvider formatProvider, String format, params Object[] args);
        void FatalFormat(Exception exception, IFormatProvider formatProvider, String format, params Object[] args);
        void Info(String message);
        void Info(Func<string> messageFactory);
        void Info(String message, Exception exception);
        void InfoFormat(String format, params Object[] args);
        void InfoFormat(Exception exception, String format, params Object[] args);
        void InfoFormat(IFormatProvider formatProvider, String format, params Object[] args);
        void InfoFormat(Exception exception, IFormatProvider formatProvider, String format, params Object[] args);
        void Warn(String message);
        void Warn(Func<string> messageFactory);
        void Warn(String message, Exception exception);
        void WarnFormat(String format, params Object[] args);
        void WarnFormat(Exception exception, String format, params Object[] args);
        void WarnFormat(IFormatProvider formatProvider, String format, params Object[] args);
        void WarnFormat(Exception exception, IFormatProvider formatProvider, String format, params Object[] args);
    }

    [Serializable]
    public class Log4NetLoggerBase : ILoggerBase
    {
        private static readonly Type declaringType = typeof(Log4NetLoggerBase);

        public Log4NetLoggerBase(log4net.Core.ILogger logger)
        {
            Logger = logger;
        }

        internal Log4NetLoggerBase()
        {
        }

        public bool IsDebugEnabled
        {
            get { return Logger.IsEnabledFor(Level.Debug); }
        }

        public bool IsErrorEnabled
        {
            get { return Logger.IsEnabledFor(Level.Error); }
        }

        public bool IsFatalEnabled
        {
            get { return Logger.IsEnabledFor(Level.Fatal); }
        }

        public bool IsInfoEnabled
        {
            get { return Logger.IsEnabledFor(Level.Info); }
        }

        public bool IsWarnEnabled
        {
            get { return Logger.IsEnabledFor(Level.Warn); }
        }

        protected internal log4net.Core.ILogger Logger { get; set; }

        public override string ToString()
        {
            return Logger.ToString();
        }

        public void Debug(String message)
        {
            if (IsDebugEnabled)
            {
                Logger.Log(declaringType, Level.Debug, message, null);
            }
        }

        public void Debug(Func<string> messageFactory)
        {
            if (IsDebugEnabled)
            {
                Logger.Log(declaringType, Level.Debug, messageFactory.Invoke(), null);
            }
        }

        public void Debug(String message, Exception exception)
        {
            if (IsDebugEnabled)
            {
                Logger.Log(declaringType, Level.Debug, message, exception);
            }
        }

        public void DebugFormat(String format, params Object[] args)
        {
            if (IsDebugEnabled)
            {
                Logger.Log(declaringType, Level.Debug, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        public void DebugFormat(Exception exception, String format, params Object[] args)
        {
            if (IsDebugEnabled)
            {
                Logger.Log(declaringType, Level.Debug, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), exception);
            }
        }

        public void DebugFormat(IFormatProvider formatProvider, String format, params Object[] args)
        {
            if (IsDebugEnabled)
            {
                Logger.Log(declaringType, Level.Debug, new SystemStringFormat(formatProvider, format, args), null);
            }
        }

        public void DebugFormat(Exception exception, IFormatProvider formatProvider, String format, params Object[] args)
        {
            if (IsDebugEnabled)
            {
                Logger.Log(declaringType, Level.Debug, new SystemStringFormat(formatProvider, format, args), exception);
            }
        }

        public void Error(String message)
        {
            if (IsErrorEnabled)
            {
                Logger.Log(declaringType, Level.Error, message, null);
            }
        }

        public void Error(Func<string> messageFactory)
        {
            if (IsErrorEnabled)
            {
                Logger.Log(declaringType, Level.Error, messageFactory.Invoke(), null);
            }
        }

        public void Error(String message, Exception exception)
        {
            if (IsErrorEnabled)
            {
                Logger.Log(declaringType, Level.Error, message, exception);
            }
        }

        public void ErrorFormat(String format, params Object[] args)
        {
            if (IsErrorEnabled)
            {
                Logger.Log(declaringType, Level.Error, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        public void ErrorFormat(Exception exception, String format, params Object[] args)
        {
            if (IsErrorEnabled)
            {
                Logger.Log(declaringType, Level.Error, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), exception);
            }
        }

        public void ErrorFormat(IFormatProvider formatProvider, String format, params Object[] args)
        {
            if (IsErrorEnabled)
            {
                Logger.Log(declaringType, Level.Error, new SystemStringFormat(formatProvider, format, args), null);
            }
        }

        public void ErrorFormat(Exception exception, IFormatProvider formatProvider, String format, params Object[] args)
        {
            if (IsErrorEnabled)
            {
                Logger.Log(declaringType, Level.Error, new SystemStringFormat(formatProvider, format, args), exception);
            }
        }

        public void Fatal(String message)
        {
            if (IsFatalEnabled)
            {
                Logger.Log(declaringType, Level.Fatal, message, null);
            }
        }

        public void Fatal(Func<string> messageFactory)
        {
            if (IsFatalEnabled)
            {
                Logger.Log(declaringType, Level.Fatal, messageFactory.Invoke(), null);
            }
        }

        public void Fatal(String message, Exception exception)
        {
            if (IsFatalEnabled)
            {
                Logger.Log(declaringType, Level.Fatal, message, exception);
            }
        }

        public void FatalFormat(String format, params Object[] args)
        {
            if (IsFatalEnabled)
            {
                Logger.Log(declaringType, Level.Fatal, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        public void FatalFormat(Exception exception, String format, params Object[] args)
        {
            if (IsFatalEnabled)
            {
                Logger.Log(declaringType, Level.Fatal, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), exception);
            }
        }

        public void FatalFormat(IFormatProvider formatProvider, String format, params Object[] args)
        {
            if (IsFatalEnabled)
            {
                Logger.Log(declaringType, Level.Fatal, new SystemStringFormat(formatProvider, format, args), null);
            }
        }

        public void FatalFormat(Exception exception, IFormatProvider formatProvider, String format, params Object[] args)
        {
            if (IsFatalEnabled)
            {
                Logger.Log(declaringType, Level.Fatal, new SystemStringFormat(formatProvider, format, args), exception);
            }
        }

        public void Info(String message)
        {
            if (IsInfoEnabled)
            {
                Logger.Log(declaringType, Level.Info, message, null);
            }
        }

        public void Info(Func<string> messageFactory)
        {
            if (IsInfoEnabled)
            {
                Logger.Log(declaringType, Level.Info, messageFactory.Invoke(), null);
            }
        }

        public void Info(String message, Exception exception)
        {
            if (IsInfoEnabled)
            {
                Logger.Log(declaringType, Level.Info, message, exception);
            }
        }

        public void InfoFormat(String format, params Object[] args)
        {
            if (IsInfoEnabled)
            {
                Logger.Log(declaringType, Level.Info, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        public void InfoFormat(Exception exception, String format, params Object[] args)
        {
            if (IsInfoEnabled)
            {
                Logger.Log(declaringType, Level.Info, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), exception);
            }
        }

        public void InfoFormat(IFormatProvider formatProvider, String format, params Object[] args)
        {
            if (IsInfoEnabled)
            {
                Logger.Log(declaringType, Level.Info, new SystemStringFormat(formatProvider, format, args), null);
            }
        }

        public void InfoFormat(Exception exception, IFormatProvider formatProvider, String format, params Object[] args)
        {
            if (IsInfoEnabled)
            {
                Logger.Log(declaringType, Level.Info, new SystemStringFormat(formatProvider, format, args), exception);
            }
        }

        public void Warn(String message)
        {
            if (IsWarnEnabled)
            {
                Logger.Log(declaringType, Level.Warn, message, null);
            }
        }

        public void Warn(Func<string> messageFactory)
        {
            if (IsWarnEnabled)
            {
                Logger.Log(declaringType, Level.Warn, messageFactory.Invoke(), null);
            }
        }

        public void Warn(String message, Exception exception)
        {
            if (IsWarnEnabled)
            {
                Logger.Log(declaringType, Level.Warn, message, exception);
            }
        }

        public void WarnFormat(String format, params Object[] args)
        {
            if (IsWarnEnabled)
            {
                Logger.Log(declaringType, Level.Warn, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), null);
            }
        }

        public void WarnFormat(Exception exception, String format, params Object[] args)
        {
            if (IsWarnEnabled)
            {
                Logger.Log(declaringType, Level.Warn, new SystemStringFormat(CultureInfo.InvariantCulture, format, args), exception);
            }
        }

        public void WarnFormat(IFormatProvider formatProvider, String format, params Object[] args)
        {
            if (IsWarnEnabled)
            {
                Logger.Log(declaringType, Level.Warn, new SystemStringFormat(formatProvider, format, args), null);
            }
        }

        public void WarnFormat(Exception exception, IFormatProvider formatProvider, String format, params Object[] args)
        {
            if (IsWarnEnabled)
            {
                Logger.Log(declaringType, Level.Warn, new SystemStringFormat(formatProvider, format, args), exception);
            }
        }
        
    }
}