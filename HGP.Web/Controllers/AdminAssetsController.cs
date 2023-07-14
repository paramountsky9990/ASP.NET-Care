using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Assets;
using HGP.Web.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HGP.Web.Controllers
{
    public class AdminAssetsControllerMappingProfile : Profile
    {
        public AdminAssetsControllerMappingProfile()
        {
            CreateMap<EditAssetModel, Asset>();
        }
    }

    [Authorize(Roles = "ClientAdmin, SuperAdmin")]
    public class AdminAssetsController : BaseController
    {
        public static ILogger Logger { get; set; }
        // GET: AdminAssets
        public AdminAssetsController(IPortalServices portalServices) : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("AdminAssetsController");
        }

        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index()
        {
            var model = this.S.SiteService.BuildAdminAssetsHomeModel(this.S.WorkContext.CurrentSite); // Don't load any data
            return View(model);
        }

        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult GetAssetsData(int rows, int page)
        {
            rows = 2000; // Asset grid preloads up to 2000 rows
            var model = this.S.SiteService.BuildAdminAssetsHomeModel(this.S.WorkContext.CurrentSite, rows, page);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        // GET: AdminAssets/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AdminAssets/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AdminAssets/Create
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

        // GET: AdminAssets/Edit/5
        public ActionResult Edit(string id)
        {
            var model = this.S.AssetService.BuildEditAssetModel(this.S.WorkContext.CurrentSite, id);
            return View(model);
        }

        // POST: AdminAssets/Edit/5
        [HttpPost]
        public ActionResult Edit(EditAssetModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var asset = this.S.AssetService.GetById(model.AsssetId);
                    Mapper.Map<EditAssetModel, Asset>(model, asset);

                    this.S.AssetService.Save(asset);

                    this.S.SiteService.UpdateCategories(asset.PortalId);
                    this.S.SiteService.UpdateManufacturers(asset.PortalId);


                    if ((List<AlertMessage>)TempData["messages"] != null)
                        ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Asset successfully updated" });

                    return RedirectToRoute("PortalRoute", new { controller = "AdminAssets", action = "Index" });
                }

                return View("~/views/adminassets/edit.cshtml", model);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        // GET: AdminAssets/Delete/5
        public ActionResult GridDeleteAssets(string idsToDelete)
        {
            var jsonIds = JsonConvert.DeserializeObject(idsToDelete);
            // you've got a list of strings so you can loop through them
            string[] ids = ((JArray)jsonIds)
                .Select(x => x.Value<string>())
                .ToArray();

            this.S.AssetService.DeleteAssets(this.S.WorkContext.CurrentSite.Id, ids);
            return Json(new { success = true });
        }

        // POST: AdminAssets/Delete/5
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

