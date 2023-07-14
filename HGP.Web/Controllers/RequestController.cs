using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using HGP.Common;
using HGP.Common.Logging;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Assets;
using HGP.Web.Models.Requests;
using HGP.Web.Services;
using HGP.Web.Utilities;

namespace HGP.Web.Controllers
{
    public class RequestControllerMappingProfile : Profile
    {
        public RequestControllerMappingProfile()
        {
            CreateMap<Address, Address>();
        }
    }

    [Authorize]
    public class RequestController : BaseController
    {
        public static ILogger Logger { get; set; }

        public RequestController(IPortalServices portalServices) : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("RequestController");
        }

        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        // GET: Request
        public ActionResult Index()
        {
            var model = this.S.RequestService.BuildRequestIndexModel(this.S.WorkContext.CurrentSite, this.S.WorkContext.CurrentUser);

            return View(model);
        }

        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        // GET: Request
        public ActionResult List(string id)
        {
            RequestListModel model = null;

            switch (id.ToLower())
            {
                case "pending":
                    model = this.S.RequestService.BuildRequestListModel(this.S.WorkContext.CurrentSite, this.S.WorkContext.CurrentUser,
                                                new[] { GlobalConstants.RequestStatusTypes.Pending });
                    break;

                case "closed":
                    model = this.S.RequestService.BuildRequestListModel(this.S.WorkContext.CurrentSite, this.S.WorkContext.CurrentUser,
                                                new[] { GlobalConstants.RequestStatusTypes.Completed, GlobalConstants.RequestStatusTypes.Approved });
                    break;
            }

            return View(model);
        }

        // GET: Request/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Request/Create
        public ActionResult Remove(string assetRequestId)
        {
            this.S.RequestService.RemoveFromRequest(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id, assetRequestId);
            return Json(new { Success = true });
        }

        // POST: Request/Create
        [HttpPost]
        public ActionResult Add(string id)
        {
            this.S.RequestService.AddToRequest(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id, id);

            return Json(new { Success = true });
        }

        [HttpPost]
        public ActionResult EditShipTo(string id, string requestorId, Address shipToAddress)
        {
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        var request = this.S.RequestService.GetById(id);
                        Mapper.Map<Address, Address>(shipToAddress, request.ShipToAddress);
                        request.IsShipToAddressValid = this.IsValidAddress(request.ShipToAddress);

                        this.S.RequestService.Save(request);

                        return Json(new { Success = true });
                    }

                    return Json(new { Success = false });
                }
                catch (Exception ex)
                {
                    throw;
                }

            }
        }

        private bool IsValidAddress(Address address)
        {
            return (!string.IsNullOrWhiteSpace(address.Street1) || !string.IsNullOrWhiteSpace(address.City) || !string.IsNullOrWhiteSpace(address.State)
                 || !string.IsNullOrWhiteSpace(address.Zip));
        }

        [HttpPost]
        public ActionResult Process(Request request)
        {
            this.S.RequestService.UpdateManager(request.Id, request.ApprovingManagerId, request.ApprovingManagerEmail, request.ApprovingManagerName, request.ApprovingManagerPhone);
            this.S.RequestService.UpdateNotes(request.Id, request.Notes);
            this.S.RequestService.Process(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id, request.Id);

            if ((List<AlertMessage>)TempData["messages"] != null)
                ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Your request has been placed. Watch your email for instructions from the asset owner." });

            return RedirectToRoute("PortalRoute", new { controller = "Portal", action = "Index" });
        }

        // GET: Request/Delete/5
        public ActionResult Approve(string id, string requestId, string message)
        {
            this.S.RequestService.ProcessDecision(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id, id, requestId, "approved", message);

            if ((List<AlertMessage>)TempData["messages"] != null)
                ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "The request has been approved and the requestor has been notified." });

            return new JsonResult() { Data = "success" };
        }

        public ActionResult Deny(string id, string requestId, string message)
        {
            this.S.RequestService.ProcessDecision(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id, id, requestId, "denied", message);

            if ((List<AlertMessage>)TempData["messages"] != null)
                ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "The request has been denied and the requestor has been notified." });

            return new JsonResult() { Data = "success" };
        }


        [HttpPost]
        public ActionResult SendReminder(string requestId)
        {
            this.S.RequestService.SendReminder(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id, requestId);

            return Json(new { Success = true });
        }

        [ChildActionOnly]
        public ActionResult SearchForMatchedAsset(string assetID)
        {
            string WishListID = string.Empty;
            try
            {
                WishListID = this.S.WishListService.GetWishListByAssetID(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id, assetID);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }

            return Content(WishListID);
        }


    }
}
