using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using AutoMapper;
using HGP.Common;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Models;
using HGP.Web.Models.Assets;
using HGP.Web.Models.Drafts;
using HGP.Web.Models.InBox;
using HGP.Web.Utilities;
using Microsoft.AspNet.Identity;
using MongoDB.Driver.Linq;

namespace HGP.Web.Services
{
    public interface IDraftAssetService : IBaseService
    {
        void Save(DraftAsset entry);
        DraftAsset GetById(string id);
        DraftAsset GetByHitNumber(string siteId, string hitNumber);
        void RemoveByHitNumber(string siteId, string hitNumber);
        DraftsIndexModel BuildDraftsHomeModel(string currentSiteId, string currentUserId);
        DraftCreateModel BuildDraftCreateModel(ISite currentSite, string draftAssetId);
        void Process(string siteId, string draftAssetId);
        int AttachFile(ISite site, PortalUser user, string hitNumber, string fileName, short sequenceNumber, MemoryStream memStream, UploadMediaFilesModel photosModel);
        void RemoveImage(string currentSiteId, string currentUserId, string fileName, string hitNumber);
        void ApproveDraftAsset(string currentSiteId, string currentUserId, string draftAssetHitNumber);
        void DenyDraftAsset(string currentSiteId, string currentUserId, string draftAssetHitNumber, string message);
    }


    public class DraftAssetServiceMappingProfile : Profile
    {
        public DraftAssetServiceMappingProfile()
        {
            CreateMap<DraftAsset, Asset>();
            CreateMap<DraftAsset, DraftCreateModel>();
            CreateMap<MediaFile, MediaFileDto>();
        }
    }

    public class DraftAssetService : BaseService<DraftAsset>, IDraftAssetService
    {
        public static ILogger Logger { get; set; }
        private IAwsService AwsService;
        private IEmailService EmailService;

        public DraftAssetService()
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("DraftAssetService");
            this.AwsService = IoC.Container.GetInstance<IAwsService>();
            Contract.Assert(this.AwsService != null);
            this.EmailService = IoC.Container.GetInstance<IEmailService>();
            Contract.Assert(this.EmailService != null);
        }

        public DraftAsset GetByHitNumber(string siteId, string hitNumber)
        {
            return this.Repository.GetAll<DraftAsset>().FirstOrDefault(x => x.PortalId == siteId && x.HitNumber == hitNumber);
        }

        public void RemoveByHitNumber(string siteId, string hitNumber)
        {
            var draftAsset = this.GetByHitNumber(siteId, hitNumber);
            this.Delete(draftAsset.Id);
        }

        public DraftsIndexModel BuildDraftsHomeModel(string currentSiteId, string currentUserId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<DraftsIndexModel>();

            var draftAssets = (from a in this.Repository.All<DraftAsset>()
                where a.PortalId == currentSiteId && a.OwnerId == currentUserId
                orderby a.UpdatedDate descending
                select a).ToList();

            model.CurrentUserId = currentUserId;

            model.DraftAssets = draftAssets;

            return model;
        }

        public DraftCreateModel BuildDraftCreateModel(ISite currentSite, string draftAssetId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<DraftCreateModel>();

            var draftAsset = (from a in this.Repository.All<DraftAsset>()
                where a.PortalId == currentSite.Id && a.OwnerId == this.WorkContext.CurrentUser.Id &&
                      a.HitNumber == draftAssetId
                select a).First();

            model.Categories = currentSite.Categories.Select(x => x.Name).ToList();
            model.Locations = currentSite.Locations.Select(x => x.Name).ToList();

            Mapper.Map<DraftAsset, DraftCreateModel>(draftAsset, model);

            foreach (var mediaFileDto in model.Media)
            {
                // Url looks like:  https://s3-us-west-1.amazonaws.com/hgpmedia/portalTag/drafts/userId/imgType/fileName
            
                mediaFileDto.ImageUrl =
                    $"{currentSite.SiteSettings.BaseImagesPath}/{currentSite.SiteSettings.PortalTag}/drafts/{this.WorkContext.CurrentUser.Id}/i/{mediaFileDto.FileName}";
                mediaFileDto.ThumbnailUrl =
                    $"{currentSite.SiteSettings.BaseImagesPath}/{currentSite.SiteSettings.PortalTag}/drafts/{this.WorkContext.CurrentUser.Id}/t/{mediaFileDto.FileName}";
            }

            return model;
        }

        public void Process(string siteId, string draftHitNumber)
        {
            var draftAsset = this.WorkContext.S.DraftAssetService.GetByHitNumber(siteId, draftHitNumber);
            var site = this.WorkContext.S.SiteService.GetById(draftAsset.PortalId);

            switch (draftAsset.DraftStatus)
            {
                case GlobalConstants.DraftAssetStatusTypes.Approved:
                    // Nothing to do here, draft already approved
                    break;

                case GlobalConstants.DraftAssetStatusTypes.OpenForEditing:
                case GlobalConstants.DraftAssetStatusTypes.SentBackForEdits: // Item was sent back and is being resubmitted
                    // Send for approval

                    DraftAssetInboxItem inboxItem = null;
                    if (draftAsset.DraftStatus == GlobalConstants.DraftAssetStatusTypes.SentBackForEdits)
                        inboxItem = this.WorkContext.S.DraftAssetInboxService.GetByHitNumber(draftAsset.PortalId, draftAsset.HitNumber);
                    if (inboxItem == null)
                     inboxItem = new DraftAssetInboxItem()
                        {
                            PortalId = site.Id,
                            CreatedBy = this.WorkContext.CurrentUser.Id,
                            CreatedDate = DateTime.UtcNow,
                            DraftAssetHitNumber = draftAsset.HitNumber,
                            DraftAsset = draftAsset                        
                        };

                    inboxItem.Status = GlobalConstants.InboxStatusTypes.Pending;
                    inboxItem.Type = GlobalConstants.InboxItemTypes.DraftAssetApprovalDecision;
      
                    this.WorkContext.S.DraftAssetInboxService.Save(inboxItem);

                    // Reload from db 
                    draftAsset.DraftStatus = GlobalConstants.DraftAssetStatusTypes.SubmittedForApproval;
                    this.WorkContext.S.DraftAssetService.Save(draftAsset);

                    IoC.Container.GetInstance<IActivityLogService>().LogActivity(GlobalConstants.ActivityTypes.SubmitDraftFDorApproval, site, this.WorkContext.CurrentUser, draftAsset.HitNumber);

                    // Send email to approvers
                    this.WorkContext.S.EmailService.SendDraftAssetPendingApproval(site, draftAsset, HttpContext.Current);
                    break;
            }
        }

        public int AttachFile(ISite site, PortalUser user, string hitNumber, string fileName, short sequenceNumber, MemoryStream memStream, UploadMediaFilesModel photosModel)
        {
            var result = 0;

            var targetAsset = this.Repository.GetQuery<DraftAsset>().AsQueryable().FirstOrDefault(a => a.PortalId == site.Id && hitNumber == a.HitNumber);
            if (targetAsset != null) // Make sure the target asset exists
            {
                var mediaFile = new MediaFile()
                {
                    ContentType = FileNameUtilities.GetContentTypeFromExtension(fileName),
                    FileName = Path.GetFileName(fileName),
                    FileData = memStream.ToArray(),
                    SortOrder = sequenceNumber,
                    IsImage = FileNameUtilities.IsImageFromExtension(fileName)
                };

                memStream.Seek(0, SeekOrigin.Begin);
                mediaFile.ThumbnailData = new ImageUtilities().GenerateThumbNail(memStream, ImageFormat.Jpeg, 0, 64).GetBuffer();

                this.AwsService.PutUserFile(site.SiteSettings.PortalTag, user.Id, "i", mediaFile.FileName, FileNameUtilities.GetContentTypeFromExtension(mediaFile.FileName), memStream);
                this.AwsService.PutUserFile(site.SiteSettings.PortalTag, user.Id, "t", mediaFile.FileName, FileNameUtilities.GetContentTypeFromExtension(mediaFile.FileName), new MemoryStream(mediaFile.ThumbnailData));

                var mediaFileDto = Mapper.Map<MediaFile, MediaFileDto>(mediaFile);
                var existingMediaFiles = targetAsset.Media.Where(x => x.FileName.ToLower() == mediaFileDto.FileName.ToLower());
                if (!existingMediaFiles.Any())
                {
                    this.AttachMedia(targetAsset, mediaFileDto);
                    result++;
                    this.Save(targetAsset);
                }
            }
            else
                Logger.Information(" Asset not found {0}", hitNumber);

            return result;
        }

        public void RemoveImage(string currentSiteId, string currentUserId, string fileName, string hitNumber)
        {
            // todo: Handle removing images
        }

        public void ApproveDraftAsset(string currentSiteId, string currentUserId, string draftAssetHitNumber)
        {
            var site = this.WorkContext.S.SiteService.GetById(currentSiteId);
            var draftAsset = this.WorkContext.S.DraftAssetService.GetByHitNumber(currentSiteId, draftAssetHitNumber);
            var userManager = IoC.Container.GetInstance<PortalUserService>();
            var assetOwner = userManager.FindById(draftAsset.OwnerId);

            // Copy the draft asset into a real Asset
            var asset = Mapper.Map<DraftAsset, Asset>(draftAsset);
            asset.Status = GlobalConstants.AssetStatusTypes.Available;
            asset.AvailForRedeploy = DateTime.Now;
            asset.AvailForSale = DateTime.Now.AddDays(30);
            asset.IsFromDraftAsset = true;

            // Save the deny message so the person who added the asset can see it
            draftAsset.ApprovedDate = DateTime.UtcNow;
            draftAsset.DraftStatus = GlobalConstants.DraftAssetStatusTypes.Approved;
            this.WorkContext.S.DraftAssetService.Save(draftAsset);

            // Email the person who added the asset
            this.EmailService.SendDraftAssetApprovedMessage(site, assetOwner, draftAsset, this.WorkContext.HttpContext);

            //Move the images from the user's bucket to the live bucket
            var imageCount = 0;
            foreach (var mediaFileDto in asset.Media)
            {
                var newFileName = "";
                if (imageCount == 0)
                    newFileName = draftAssetHitNumber + Path.GetExtension(mediaFileDto.FileName);
                else
                    newFileName = draftAssetHitNumber + "-" + imageCount.ToString() + Path.GetExtension(mediaFileDto.FileName);

                this.CopyDraftImageToLiveBucket(site.SiteSettings.PortalTag, assetOwner.Id, mediaFileDto.FileName, newFileName);
                mediaFileDto.FileName = newFileName;
                imageCount++;
            }

            this.WorkContext.S.AssetService.Save(asset);

            // remove the inbox item, no need to keep it
            var inboxItem = this.WorkContext.S.DraftAssetInboxService.GetByHitNumber(draftAsset.PortalId, draftAssetHitNumber);
            this.WorkContext.S.DraftAssetInboxService.Delete(inboxItem.Id);

            this.WorkContext.S.SiteService.UpdateCategories(site.Id);
            this.WorkContext.S.SiteService.UpdateManufacturers(site.Id);

            IoC.Container.GetInstance<IActivityLogService>().LogActivity(GlobalConstants.ActivityTypes.DenyDraftAsset, site, this.WorkContext.CurrentUser, draftAsset.HitNumber);
        }

        private void CopyDraftImageToLiveBucket(string portalTag, string userId, string fileName, string newFileName)
        {
            var srcStream = this.AwsService.GetFile(portalTag, "/drafts/" + userId + "/i", fileName);
            var memoryStream = new MemoryStream();

            using (var br = new BinaryReader(srcStream))
                memoryStream.Write(br.ReadBytes((int)srcStream.Length), 0,
                    (int)srcStream.Length);

            memoryStream.Seek(0, SeekOrigin.Begin);
            var thumbnailData = new ImageUtilities().GenerateThumbNail(memoryStream, ImageFormat.Jpeg, 0, 64).GetBuffer();
            memoryStream.Seek(0, SeekOrigin.Begin);
            var largeThumbnailData = new ImageUtilities().GenerateThumbNail(memoryStream, ImageFormat.Jpeg, 0, 225).GetBuffer();

            this.AwsService.PutFile(portalTag, "i", newFileName, FileNameUtilities.GetContentTypeFromExtension(newFileName), memoryStream);
            this.AwsService.PutFile(portalTag, "t", newFileName, FileNameUtilities.GetContentTypeFromExtension(newFileName), new MemoryStream(thumbnailData));
            this.AwsService.PutFile(portalTag, "l", newFileName, FileNameUtilities.GetContentTypeFromExtension(newFileName), new MemoryStream(largeThumbnailData));
        }

        public void DenyDraftAsset(string currentSiteId, string currentUserId, string draftAssetHitNumber, string message)
        {
            var site = this.WorkContext.S.SiteService.GetById(currentSiteId);
            var draftAsset = this.WorkContext.S.DraftAssetService.GetByHitNumber(currentSiteId, draftAssetHitNumber);
            var userManager = IoC.Container.GetInstance<PortalUserService>();
            var assetOwner = userManager.FindById(draftAsset.OwnerId);

            // Save the deny message so the person who added the asset can see it
            draftAsset.DraftStatus = GlobalConstants.DraftAssetStatusTypes.SentBackForEdits;
            draftAsset.Notes = message;
            this.WorkContext.S.DraftAssetService.Save(draftAsset);

            // Email the person who added the asset
            this.EmailService.SendDraftAssetDeniedApproval(site, draftAsset, message, this.WorkContext.HttpContext);

            // remove the inbox item, no need to keep it
            var inboxItem = this.WorkContext.S.DraftAssetInboxService.GetByHitNumber(draftAsset.PortalId, draftAssetHitNumber);
            this.WorkContext.S.DraftAssetInboxService.Delete(inboxItem.Id);

            IoC.Container.GetInstance<IActivityLogService>().LogActivity(GlobalConstants.ActivityTypes.DenyDraftAsset, site, this.WorkContext.CurrentUser, draftAsset.HitNumber);
        }

        public void AttachMedia(DraftAsset asset, MediaFileDto file)
        {
            if (asset.Media == null)
                asset.Media = new List<MediaFileDto>();
            asset.Media.Add(file);
            asset.Media = asset.Media.OrderBy(x => x.SortOrder).ToList();
            //this.Save(asset); Saving the asset is handled by the Create web form
        }
    }
}