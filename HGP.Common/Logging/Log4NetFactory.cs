//using System;
//using System.Configuration;
//using AuctionCMS.Framework.Logging;
//using log4net;
//using log4net.Config;

//namespace Orchard.Logging
//{
//    public class Log4NetFactory : log4net.Core.ILogger
//    {
//        private static bool _isFileWatched = false;

//        //public Log4NetFactory(string configFilename, IHostEnvironment hostEnvironment)
//        //{
//        //    if (!_isFileWatched && !string.IsNullOrWhiteSpace(configFilename) && hostEnvironment.IsFullTrust)
//        //    {
//        //        // Only monitor configuration file in full trust
//        //        XmlConfigurator.ConfigureAndWatch(GetConfigFile(configFilename));
//        //        _isFileWatched = true;
//        //    }
//        //}

//        //public override Castle.Core.Logging.ILogger Create(string name, LoggerLevel level)
//        //{
//        //    throw new NotSupportedException("Logger levels cannot be set at runtime. Please review your configuration file.");
//        //}

//        public ILogger Create(string name)
//        {
//            return new log4net.(LogManager.GetLogger(name), this);
//        }
//    }
//}
