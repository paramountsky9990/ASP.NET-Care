#region Using

using System.Collections.Generic;
using System.Web.Mvc;
using HGP.Common.Logging;
using HGP.Web.Models;
using HGP.Web.Services;

#endregion

namespace HGP.Web.Controllers
{
    [Authorize]
    public class InBoxController : BaseController
    {
        public InBoxController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("InBoxController");
        }

        public static ILogger Logger { get; set; }

        // GET: List
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index()
        {
            var model = S.InBoxService.BuildInBoxHomeModel(S.WorkContext.CurrentSite.Id, S.WorkContext.CurrentUser.Id);

            var menuItems = new List<MenuItem>
            {
                new MenuItem
                {
                    ActionName = "index",
                    ControllerName = "inbox",
                    IsActive = true,
                    LinkText = string.Format("Inbox ({0})", model.InBoxCount),
                },
                new MenuItem
                {
                    ActionName = "index",
                    ControllerName = "inbox",
                    IsActive = false,
                    LinkText = string.Format("Pending ({0})", 0),
                },
                new MenuItem
                {
                    ActionName = "index",
                    ControllerName = "inbox",
                    IsActive = false,
                    LinkText = string.Format("Complete ({0})", 0),
                }
            };

            model.MenuItems.AddRange(menuItems);

            return View(model);
        }
    }
}