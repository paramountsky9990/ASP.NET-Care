using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using HGP.Common.Logging;
using HGP.Web.Models;
using HGP.Web.Models.Report;
using HGP.Web.Services;
using Microsoft.Ajax.Utilities;

namespace HGP.Web.Controllers
{
    [Authorize]
    public class ReportController : BaseController
    {
        public static ILogger Logger { get; set; }

        public ReportController(IPortalServices portalServices)
              : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("ReportController");            
        }
            // GET: Report
        public ActionResult Disposition()
        {
            var model = this.S.AssetService.BuildDispositionReportModel(this.S.WorkContext.CurrentSite.Id);
            return View(model);
        }

        public ActionResult DispositionData()
        {
            var model = this.S.AssetService.BuildDispositionReportDataModel(this.S.WorkContext.CurrentSite.Id);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DispositionDataExcel()
        {
            var model = this.S.AssetService.BuildDispositionReportDataModel(this.S.WorkContext.CurrentSite.Id);

            return new ExcelResult<IList<AssetDispositionLineItem>>(model);
        }


        public ActionResult AllAssets()
        {
            var model = this.S.AssetService.BuildAllAssetsReportModel(this.S.WorkContext.CurrentSite.Id);
            return View(model);
        }
        public ActionResult AllAssetsData()
        {
            var model = this.S.AssetService.BuildAllAssetsReportDataModel(this.S.WorkContext.CurrentSite.Id);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AllAssetsDataExcel()
        {
            var model = this.S.AssetService.BuildAllAssetsReportDataModel(this.S.WorkContext.CurrentSite.Id);

            return new ExcelResult<IList<AllAssetReportLineItemModel>>(model);
        }

        public ActionResult AvailableAssets()
        {
            var model = this.S.AssetService.BuildAvailableAssetsReportModel(this.S.WorkContext.CurrentSite.Id);
            return View(model);
        }
        
        public ActionResult AvailableAssetsData()
        {
            var model = this.S.AssetService.BuildAvailableAssetsReportDataModel(this.S.WorkContext.CurrentSite.Id);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AvailableAssetsDataExcel()
        {
            var model = this.S.AssetService.BuildAvailableAssetsReportDataModel(this.S.WorkContext.CurrentSite.Id);

            return new ExcelResult<IList<AllAssetReportLineItemModel>>(model);
        } 
        
        public ActionResult ExpiredAssets()
        {
            var model = this.S.AssetService.BuildExpiredAssetsReportModel(this.S.WorkContext.CurrentSite.Id);
            return View(model);
        }
        public ActionResult ExpiredAssetsData()
        {
            var model = this.S.AssetService.BuildExpiredAssetsReportDataModel(this.S.WorkContext.CurrentSite.Id);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ExpiredAssetsDataExcel()
        {
            var model = this.S.AssetService.BuildExpiredAssetsReportDataModel(this.S.WorkContext.CurrentSite.Id);

            return new ExcelResult<IList<ExpiredAssetReportLineItemModel>>(model);
        }


        public ActionResult AllRequests()
        {
            var model = this.S.RequestService.BuildAllRequestsReportModel(this.S.WorkContext.CurrentSite.Id);
            return View(model);
        }
        public ActionResult AllRequestsData()
        {
            var model = this.S.RequestService.BuildAllRequestsReportDataModel(this.S.WorkContext.CurrentSite.Id);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AllRequestsDataExcel()
        {
            var model = this.S.RequestService.BuildAllRequestsReportDataModel(this.S.WorkContext.CurrentSite.Id);

            return new ExcelResult<IList<AllRequestsReportLineItemModel>>(model);
        }

        public ActionResult AllWishes()
        {
            var model = this.S.WishListService.BuildAllWishesReportModel(this.S.WorkContext.CurrentSite.Id);
            return View(model);
        }
        public ActionResult AllWishesData()
        {
            var model = this.S.WishListService.BuildAllWishesReportDataModel(this.S.WorkContext.CurrentSite.Id);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AllWishesDataExcel()
        {
            var model = this.S.WishListService.BuildAllWishesReportDataModel(this.S.WorkContext.CurrentSite.Id);

            return new ExcelResult<IList<AllWishesLineItemModel>>(model);
        }
    }

    public class ExcelResult<T> : ActionResult
    {
        private T data;

        public ExcelResult(T data)
        {
            this.data = data;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            HttpResponseBase response = context.HttpContext.Response;

            response.ContentType = "application/excel";
            response.AddHeader("content-disposition", "attachment; filename=MyExcelFile.xls");

            if (data != null)
            {
                var sw = new StringWriter();

                var htw = new HtmlTextWriter(sw);

                var grid = new GridView { DataSource = data };
                grid.DataBind();
                grid.RenderControl(htw);

                response.Write(sw.ToString());

                response.End();
            }
        }
    }

}
