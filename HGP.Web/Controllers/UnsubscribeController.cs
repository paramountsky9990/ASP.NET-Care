using HGP.Common.Logging;
using HGP.Web.Models;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using HGP.Common;

namespace HGP.Web.Controllers
{
    [Authorize]
    public class UnsubscribeController : BaseController
    {
        public static ILogger Logger { get; set; }

        public UnsubscribeController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("UnsubscribeController");
        }

        // GET: UnsubscribeMails
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        [HttpGet]
        public ActionResult Index()
        {
            var model = this.S.UnsubscribeService.BuildUnsubscribeHomeModel();
            return View(model);
        }
      
        [HttpPost]
        public ActionResult Index(UnsubscribeHomeModel unsubMail)
        {
            bool res = false;
            try
            {
                res = this.S.UnsubscribeService.Add(unsubMail.Unsubscribe);
                if (res)
                {
                    if(unsubMail.Unsubscribe.MailType == GlobalConstants.UnsubscribeTypes.ReceiveAll)
                    {
                        DisplaySuccessMessage("You have successfully subscribed to all emails.");
                    }
                    else
                    {
                        DisplaySuccessMessage("You have been removed from the email list and will no longer receive daily or weekly messages.");
                    }                  
                }
                else
                    DisplayErrorMessage("An error occured. Please try again later.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }

            return RedirectToRoute("PortalRoute", new { controller = "Portal", action = "Index" });
        }

    }
}