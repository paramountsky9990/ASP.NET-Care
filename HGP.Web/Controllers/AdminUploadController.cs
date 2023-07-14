using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HGP.Common.Logging;
using HGP.Web.Services;

namespace HGP.Web.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AdminUploadController : BaseController
    {
         public static ILogger Logger { get; set; }

         public AdminUploadController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("AdminUploadController");            
        }


         [HttpPost]
         public ActionResult UploadLogoAdmin(string siteId)
         {
             var site = this.S.SiteService.GetById(siteId);

             return RedirectToAction("UploadLogoAdmin", "Upload");
         }
    }
}