using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HGP.Common.Logging;
using HGP.Web.Models;
using HGP.Web.Services;

namespace HGP.Web.Controllers
{
    [Authorize]
    public class PortalController : BaseController
    {
        public static ILogger Logger { get; set; }

        public PortalController(IPortalServices portalServices) : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("PortalController");            
        }
        
        // GET: Portal
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index()
        {
            var model = this.S.SiteService.BuildPortalHomeModel(this.S.WorkContext.CurrentSite, this.S.WorkContext.CurrentUser);
            return View(model);
        }

        // GET: Portal/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Portal/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Portal/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Portal/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Portal/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Portal/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Portal/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
