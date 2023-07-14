using System;
using HGP.Common.Logging;

namespace HGP.Web.Utilities
{
    public class TimedLogEntry : IDisposable
    {
        private string _Message;
        private long _StartTicks;
        private bool _Enabled;
        public static ILogger Logger { get; set; }

        public TimedLogEntry(string userName, string message, bool enabled)
        {
            Logger = Log4NetLogger.GetLogger();
            
            this._Message = userName + '\t' + message;
            this._StartTicks = DateTime.Now.Ticks;
            this._Enabled = enabled;
        }

        public TimedLogEntry(string userName, string message)
        {
            Logger = Log4NetLogger.GetLogger();

            this._Message = userName + '\t' + message;
            this._StartTicks = DateTime.Now.Ticks;
            this._Enabled = true;
        }


        public TimedLogEntry(string format, params object[] args)
        {
            Logger = Log4NetLogger.GetLogger();

            this._Message = string.Format(format, args);
            this._StartTicks = DateTime.Now.Ticks;
            this._Enabled = true;
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            if (_Enabled)
            {
                string msg = this._Message + ' ' + TimeSpan.FromTicks(DateTime.Now.Ticks - this._StartTicks).TotalSeconds.ToString();
                //EntLibHelper.PerformanceLog(msg);
                Logger.Information(msg);
            }
        }

        #endregion
    }
}


