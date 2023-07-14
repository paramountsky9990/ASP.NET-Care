#region Using

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using HGP.Common;
using HGP.Common.Logging;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Assets;
using HGP.Web.Models.Drafts;
using HGP.Web.Services;

#endregion

namespace HGP.Web.Controllers
{
    [Authorize]
    public class DraftsController : BaseController
    {
        public DraftsController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("DraftsController");
        }

        public static ILogger Logger { get; set; }

        // GET: List
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index()
        {
            var model = S.DraftAssetService.BuildDraftsHomeModel(S.WorkContext.CurrentSite.Id, S.WorkContext.CurrentUser.Id);
            if (model.DraftAssets.Count() == 0)
            {
                // No open draft found, create one

                var newAsset = CreateDraft();

                model.DraftAssets.Add(newAsset);

                return RedirectToRoute("PortalRoute", new { controller = "drafts", action = "create", draftId = newAsset.HitNumber });
            }

            return View(model);
        }

        private DraftAsset CreateDraft()
        {
            // Generate a random number for the key
            TimeSpan span = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var randomKey = (int) span.TotalSeconds;

            var newAsset = new DraftAsset()
            {
                BookValue = "0",
                HitNumber = randomKey.ToString(),
                DraftStatus = GlobalConstants.DraftAssetStatusTypes.OpenForEditing,
                Status = GlobalConstants.AssetStatusTypes.Available,
                Media = new List<MediaFileDto>(),
                OwnerId = S.WorkContext.CurrentUser.Id,
                OwnerFirstName = S.WorkContext.CurrentUser.FirstName,
                OwnerLastName = S.WorkContext.CurrentUser.LastName,
                OwnerEmail = this.S.WorkContext.CurrentUser.Email,
                OwnerPhone = this.S.WorkContext.CurrentUser.PhoneNumber,
                PortalId = S.WorkContext.CurrentSite.Id,
                CreatedBy = S.WorkContext.CurrentUser.Id,
                UpdatedBy = S.WorkContext.CurrentUser.Id,
                IsVisible = true,
                DisplayBookValue = true
            };
            S.DraftAssetService.Save(newAsset);
            return newAsset;
        }

        public ActionResult CreateNewDraft()
        {
            var newAsset = CreateDraft();

            return RedirectToRoute("PortalRoute", new { controller = "drafts", action = "create", draftId = newAsset.HitNumber });
        }

        // GET: Asset/Create
        public ActionResult Create(string draftId)
        {
            var model = this.S.DraftAssetService.BuildDraftCreateModel(this.S.WorkContext.CurrentSite, draftId);
            return View("Create", model);
        }

        // POST: Asset/Create
        [HttpPost]
        public ActionResult Create(DraftCreateModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var draftAsset = this.S.DraftAssetService.GetById(model.Id);
                    Mapper.Map<DraftCreateModel, DraftAsset>(model, draftAsset);
                    draftAsset.OwnerId = this.S.WorkContext.CurrentUser.Id;
                    draftAsset.OwnerFirstName = this.S.WorkContext.CurrentUser.FirstName;
                    draftAsset.OwnerLastName = this.S.WorkContext.CurrentUser.LastName;
                    draftAsset.OwnerEmail = this.S.WorkContext.CurrentUser.Email;
                    draftAsset.OwnerPhone = this.S.WorkContext.CurrentUser.PhoneNumber;
                    this.S.DraftAssetService.Save(draftAsset);

                    if (model.SubmitForApproval)
                    {
                        this.S.DraftAssetService.Process(draftAsset.PortalId, draftAsset.HitNumber);
                        if ((List<AlertMessage>)TempData["messages"] != null)
                            ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Draft asset successfully submitted for approval." });
                    }
                    else
                    {
                        if ((List<AlertMessage>)TempData["messages"] != null)
                            ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Draft asset successfully saved." });
                    }

                    return RedirectToRoute("PortalRoute", new { controller = "drafts", action = "Index" });
                }

            }
            catch
            {
                return View("Create", model);
            }
                return View("Create", model);
        }

        public ActionResult SaveUploadedFiles(string hitNumber)
        {
            bool isSavedSuccessfully = true;

            try
            {
                short fileCount = 0;
                foreach (string fileName in Request.Files)
                {
                    fileCount++;
                    
                    HttpPostedFileBase file = Request.Files[fileName];

                    if (file != null && file.ContentLength > 0)
                    {
                        var memoryStream = new MemoryStream();
                        using (var br = new BinaryReader(file.InputStream))
                            memoryStream.Write(br.ReadBytes((int)file.InputStream.Length), 0,
                                (int)file.InputStream.Length);

                        var photosModel = new UploadMediaFilesModel();
                        var result = this.S.DraftAssetService.AttachFile(this.S.WorkContext.CurrentSite, this.S.WorkContext.CurrentUser, hitNumber, file.FileName, fileCount, memoryStream, photosModel);
                    }
                }
            }
            catch (Exception ex)
            {
                isSavedSuccessfully = false;
            }
            if (isSavedSuccessfully)
            {
                return Json(new
                {
                    Message = "" // Not sure what goes here
                });
            }
            else
            {
                return Json(new
                {
                    Message = "Error in saving file"
                });
            }
        }

        public ActionResult Process(string draftHitNumber)
        {
            this.S.DraftAssetService.Process(this.S.WorkContext.CurrentSite.Id, draftHitNumber);
            if ((List<AlertMessage>)TempData["messages"] != null)
                ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Draft asset successfully submitted for approval." });
            return RedirectToRoute("PortalRoute", new { controller = "drafts", action = "Index" });
        }

        [HttpPost]
        public ActionResult Remove(string hitNumber)
        {
            //todo: Remove images prior to deleting draft asset

            // Remove any associated InBox items
            var inBoxItem = this.S.DraftAssetInboxService.GetByHitNumber(this.S.WorkContext.CurrentSite.Id, hitNumber);
            if (inBoxItem != null)
                this.S.DraftAssetInboxService.Delete(inBoxItem.Id);

            this.S.DraftAssetService.RemoveByHitNumber(this.S.WorkContext.CurrentSite.Id, hitNumber);

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult DeleteImage(string fileName, string hitNumber)
        {
            this.S.DraftAssetService.RemoveImage(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id, fileName, hitNumber);

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult Approve(string draftAssetHitNumber)
        {
            this.S.DraftAssetService.ApproveDraftAsset(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id, draftAssetHitNumber);

            if ((List<AlertMessage>)TempData["messages"] != null)
                ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Draft asset successfully imported into portal." });

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult Deny(string draftAssetHitNumber, string message)
        {
            this.S.DraftAssetService.DenyDraftAsset(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id, draftAssetHitNumber, message);

            if ((List<AlertMessage>)TempData["messages"] != null)
                ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Draft asset denied; creator sent an email with the message." });

            return Json(new { success = true });
        }

    }
}