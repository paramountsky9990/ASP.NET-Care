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
    public class AdminHomeController : BaseController
    {
        public static ILogger Logger { get; set; }

        public AdminHomeController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("AdminHomeController");            
        }
        
        // GET: AdminHome
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index()
        {
            var model = this.S.SiteService.BuildAdminHomeModel();
            return View(model);
        }

        // GET: AdminHome/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AdminHome/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AdminHome/Create
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

        // GET: AdminHome/Edit/5
        public ActionResult Edit(string id)
        {
            return View();
        }

        // POST: AdminHome/Edit/5
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

        // GET: AdminHome/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AdminHome/Delete/5
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
