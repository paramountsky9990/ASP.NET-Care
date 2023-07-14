using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HGP.Common.Logging;
using HGP.Web.Services;
using log4net;

namespace HGP.Web.Controllers
{
    public class HomeController : BaseController
    {
        public static ILogger Logger { get; set; }

        public HomeController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("HomeController");            
        }
        
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        [Route("")]
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}