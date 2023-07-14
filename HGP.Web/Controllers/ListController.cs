#region Using

using System.Collections.Generic;
using System.Web.Mvc;
using AutoMapper;
using HGP.Common.Logging;
using HGP.Web.Extensions;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Assets;
using HGP.Web.Services;
using Newtonsoft.Json;

using HGP.Web.Models.List;
using HGP.Web.Models.Report;

#endregion

namespace HGP.Web.Controllers
{
    public class ClientMappingProfile : Profile
    {
        public ClientMappingProfile()
        {
            CreateMap<CategoryListModel, MenuItem>()
                .ForMember(dest => dest.LinkText, opts => opts.MapFrom(src => string.Format("{0} ({1})", src.Name, src.Count)))
                .ForMember(dest => dest.ControllerName, opts => opts.MapFrom(src => "list"))
                .ForMember(dest => dest.ActionName, opts => opts.MapFrom(src => "index"))
                .ForMember(dest => dest.Tag, opts => opts.MapFrom(src => src.UriString));
        }
    }

    [Authorize]
    public class ListController : BaseController
    {
        public ListController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("ListController");
        }

        public static ILogger Logger { get; set; }

        // GET: List
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index(string category = "", string location = "", string search = "")
        {
            var model = S.ListService.BuildListHomeModel((Site)S.WorkContext.CurrentSite, S.WorkContext.CurrentUser, category, location, search);

            return View("Index", model);
        }


        public ActionResult ListExcel(string category = "", string location = "", string search = "")
        {
            var currentSite = ((Site)S.WorkContext.CurrentSite);
            var model = S.ListService.ListAllAssetsDataModel(currentSite, category, location, search);
            return new ExcelResult<IList<AllAssetReportLineItemModel>>(model);           
        }


        // GET: List
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult GetAssets(string category = "", string location = "", int page = 1, int itemsPerPage = 10, string search = "")
        {
            var model = S.ListService.BuildListAssetsPage((Site)S.WorkContext.CurrentSite, category, location, search, page,
                itemsPerPage);
            var result = JsonConvert.SerializeObject(model.PagedAssets);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        [HttpPost]
        public ActionResult Search(string search, string category = "")
        {
            return Index(category, search);
        }

        // GET: List/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: List/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: List/Create
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

        // GET: List/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: List/Edit/5
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

        // GET: List/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: List/Delete/5
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