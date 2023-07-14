using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using HGP.Common.Logging;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Assets;
using HGP.Web.Models.Drafts;
using HGP.Web.Services;

namespace HGP.Web.Controllers
{
    public class AssetController : BaseController
    {
        public class AssetControllerMappingProfile : Profile
        {
            public AssetControllerMappingProfile()
            {
                CreateMap<DraftCreateModel, DraftAsset>();
            }
        }

        public static ILogger Logger { get; set; }

        public AssetController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("AssetController");            
        }
    
        // GET: List
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index(string id)
        {
            var model = this.S.AssetService.BuildAssetIndexModel(this.S.WorkContext.CurrentSite, id);
            return View("Index", model);
        }

        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult IndexPartial(string id)
        {
            var model = this.S.AssetService.BuildAssetIndexModel(this.S.WorkContext.CurrentSite, id);
            return PartialView("Index_Detail", model);
        }

        // GET: Asset/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Asset/Edit/5
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

        // GET: Asset/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Asset/Delete/5
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
