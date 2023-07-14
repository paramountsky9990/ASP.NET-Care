using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Web;
using AspNet.Identity.MongoDB;
using AutoMapper;
using Glimpse.Core.Extensions;
using HGP.Common;
using HGP.Common.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Email;
using HGP.Web.Models.List;
using HGP.Web.Models.Report;
using HGP.Web.Models.Requests;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Newtonsoft.Json;
using WebGrease.Css.Extensions;
using static HGP.Common.GlobalConstants;

namespace HGP.Web.Services
{
    public interface IRequestService
    {
        void Save(Request entry);
        Request GetById(string id);

        IMongoRepository Repository { get; }

        IList<Request> GetByUserIdStatus(string siteId, string userId, GlobalConstants.RequestStatusTypes requestStatusTypes);
        IList<Request> GetByApprovingManagerStatus(string siteId, string userId, GlobalConstants.RequestStatusTypes requestStatus);

        void RemoveFromRequest(string siteId, string requestorId, string assetRequestId);
        Request AddToRequest(string siteId, string requestorId, string hitNumber);
        Request AddToRequest(Asset asset, PortalUser requestor);

        RequestIndexModel BuildRequestIndexModel(ISite site, PortalUser portalUser);

        Request GetOpenOrNewRequest(string siteId, string userId);

        void Process(string siteId, string requestorId, string requestId);

        List<string> GetRequestedAssetIds(string siteId);
        void SetAssetRequestStatus(Request request, string assetId, GlobalConstants.RequestStatusTypes newStatus, string message = "");

        RequestListModel BuildRequestListModel(ISite site, PortalUser portalUser, GlobalConstants.RequestStatusTypes[] statusTypes);

        int GetRequestCount(string siteId, string userId);
        int GetRequestCountByStatus(string siteId, string userId, GlobalConstants.RequestStatusTypes[] statusTypes);

        void ProcessDecision(string siteId, string approverId, string assetRequestId, string requestId, string decision, string message = "");
        AllRequestsReportDataModel BuildAllRequestsReportModel(string siteId);
        IList<AllRequestsReportLineItemModel> BuildAllRequestsReportDataModel(string siteId);

        void UpdateManager(string requestId, string managerId, string managerEmail, string managerName, string managerPhone);

        void SendReminder(string siteId, string userId, string requestId);

        void UpdateNotes(string requestId, string notes);

        // Get All Pending Requests for a Portal
        List<Request> GetPendingRequests(Int32 waitingDays, Site site);

        List<Request> GetByAssetId(string siteId, string assetId);

    }

    public class RequestServiceMappingProfile : Profile
    {
        public RequestServiceMappingProfile()
        {
            CreateMap<Asset, AssetRequestDetail>();       
        }
    }

    public class RequestService : BaseService<Request>, IRequestService
    {
        private ISiteService SiteService;
        public RequestService()
            : this(null, null, null)
        {
        }

        public RequestService(IAssetService assetService, IEmailService emailService, ISiteService siteService)
        {
            this.AssetService = assetService;
            this.EmailService = emailService;
            this.SiteService = siteService;
           }

        public IList<Request> GetByUserIdStatus(string siteId, string userId, GlobalConstants.RequestStatusTypes requestStatus)
        {
            var requests = (from r in this.Repository.All<Request>()
                            where
                                r.PortalId == siteId && r.RequestorId == userId &&
                                r.Status == requestStatus
                            select r);

            return requests.ToList();
        }

        /// <summary>
        /// Returns list of inbox requests
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="userId">User for whom to display the relevant pending requests</param>
        /// <param name="requestStatus"></param>
        /// <returns></returns>
        public IList<Request> GetByApprovingManagerStatus(string siteId, string userId, GlobalConstants.RequestStatusTypes requestStatus)
        {
            List<Request> result;

            // Find requests for the user id specified (typically currently logged in user)
            result = (from r in this.Repository.All<Request>()
                      where r.PortalId == siteId && r.ApprovingManagerId == userId &&
                          r.AssetRequests.Any(x => x.Status == requestStatus)
                      select r).ToList();

            var userManager = IoC.Container.GetInstance<PortalUserService>();
            var user = userManager.FindById(userId);
            if (user != null && (user.Roles.Contains("Approver") || user.Roles.Contains("ClientAdmin") || user.Roles.Contains("SuperAdmin")))
            {
                // Find requests for any approvers in this portal
                var requestsByApprovalRole = (from r in this.Repository.All<Request>()
                                              where r.PortalId == siteId &&
                                                    r.AssetRequests.Any(x => x.Status == requestStatus)
                                              select r).ToList();

                result = result.Union(requestsByApprovalRole, new RequestEqualityComparer()).ToList();
            }

            return result;
        }


        /// <summary>
        /// Retrieves an open request for specified user. Creates a new one
        /// if necessary.
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Request GetOpenOrNewRequest(string siteId, string userId)
        {
            var site = this.Repository.All<Site>().FirstOrDefault(x => x.Id == siteId);
            if ((site == null) || (site.SiteSettings.IsAdminPortal)) // No need to save a request object for the admin portal
                return null;

            var request = (from r in this.Repository.All<Request>()
                           where
                               r.PortalId == siteId && r.RequestorId == userId &&
                               r.Status == GlobalConstants.RequestStatusTypes.Open
                           select r).FirstOrDefault();

            if (request != null && request.AssetRequests != null && request.AssetRequests.Count > 0)
            {
                IList<AssetRequestDetail> approvedAssets = new List<AssetRequestDetail>();

                foreach (AssetRequestDetail requestAsset in request.AssetRequests)
                {
                    Asset cAsset = this.Repository.All<Asset>().Where(a => a.Id == requestAsset.Id && a.Status == GlobalConstants.AssetStatusTypes.Requested).FirstOrDefault();
                    if (cAsset != null)
                    {
                        approvedAssets.Add(requestAsset);
                    }
                }

                foreach (AssetRequestDetail removeAsset in approvedAssets)
                {
                    request.AssetRequests.Remove(removeAsset);
                    request.AssetCount--;
                }

                // if (request.AssetRequests.Count <= 0)
                // {
                //     request.Status = GlobalConstants.RequestStatusTypes.Completed;
                //}
                this.Save(request);

            }



            try
            {
                // Only one open request is allowed, if there isn't one already, create it
                if (request == null)
                {
                    var requestor = this.Repository.All<PortalUser>().FirstOrDefault(x => x.Id == userId);
                    if (requestor != null)
                    {
                        // Update next request number
                        var args = new FindAndModifyArgs()
                        {
                            Query = Query.EQ("_id", BsonValue.Create(siteId)),
                            Update = Update.Inc("SiteSettings.NextRequestNum", 1),
                            VersionReturned = FindAndModifyDocumentVersion.Modified,
                        };
                        site = this.Repository.FindAndModify<Site>(args);

                        request = new Request()
                        {
                            PortalId = siteId,
                            Status = GlobalConstants.RequestStatusTypes.Open,
                            RequestorId = userId,
                            RequestorName = requestor.FirstName + " " + requestor.LastName,
                            RequestorPhone = requestor.PhoneNumber,
                            RequestorEmail = requestor.Email,
                            ShipToAddress = requestor.Address,
                            IsShipToAddressValid = this.ValidateAddress(requestor.Address),
                            AssetCount = 0,
                            RequestDate = DateTime.UtcNow,
                            RequestNum = site.SiteSettings.NextRequestNum.ToString(),
                        };

                        // If there's a previous request, copy in some data to use as defaults
                        var previousRequest = (from r in this.Repository.All<Request>()
                                               where
                                                   r.PortalId == siteId && r.RequestorId == userId &&
                                                   r.Status != GlobalConstants.RequestStatusTypes.Open
                                               orderby r.CreatedDate descending
                                               select r).FirstOrDefault();

                        if ((previousRequest != null) && (request.ApprovingManagerId != requestor.Id))
                        {
                            request.ApprovingManagerEmail = previousRequest.ApprovingManagerEmail;
                            request.ApprovingManagerId = previousRequest.ApprovingManagerId;
                            request.ApprovingManagerName = previousRequest.ApprovingManagerName;
                            request.ApprovingManagerPhone = previousRequest.ApprovingManagerPhone;
                        }

                        this.Save(request);
                    }
                }
                else
                {

                }


            }
            catch (Exception)
            {

                throw;
            }

            return request;
        }


        public List<string> GetRequestedAssetIds(string siteId)
        {
            var result = new List<string>();
            var requests = this.Repository.All<Request>().Where(x => x.PortalId == siteId);
            foreach (var request in requests)
            {
                result.AddRange(request.AssetRequests.Where(x => (x.Status != GlobalConstants.RequestStatusTypes.Open) && (x.Status != GlobalConstants.RequestStatusTypes.Denied)).Select(x => x.Id));
            }

            return result;
        }


        public void SetAssetRequestStatus(Request request, string assetId, GlobalConstants.RequestStatusTypes newStatus, string message = "")
        {
            var requestDetail = request.AssetRequests.FirstOrDefault(x => x.Id == assetId);
            if (requestDetail != null)
            {
                requestDetail.Status = newStatus;
                requestDetail.TaskComment = message;
            }
        }

        public RequestListModel BuildRequestListModel(ISite site, PortalUser portalUser, GlobalConstants.RequestStatusTypes[] statusTypes)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<RequestListModel>();

            var requests = (from r in this.AssetService.Repository.All<Request>()
                            where r.PortalId == site.Id && r.RequestorId == portalUser.Id && r.AssetCount > 0 && statusTypes.Contains(r.Status)
                            orderby r.RequestDate descending
                            select r);

            if (requests.Any())
                model.Requests = requests.ToList();

            model.JsonData = JsonConvert.SerializeObject(model);

            return model;
        }

        public int GetRequestCount(string siteId, string userId)
        {
            var requests = (from r in this.AssetService.Repository.All<Request>()
                            where r.PortalId == siteId && r.RequestorId == userId
                            select r);

            return requests.Count();
        }

        public int GetRequestCountByStatus(string siteId, string userId, GlobalConstants.RequestStatusTypes[] statusTypes)
        {
            var requests = (from r in this.AssetService.Repository.All<Request>()
                            where r.PortalId == siteId && r.RequestorId == userId && r.AssetCount > 0 && statusTypes.Contains(r.Status)
                            select r);

            return requests.Count();
        }

        private bool IsRequestClosed(Request request)
        {
            // Have we processed all asset requests?
            return request.AssetRequests.All(x => x.Status != GlobalConstants.RequestStatusTypes.Pending);
        }

        private void TryCloseRequest(Request request)
        {
            if (IsRequestClosed(request))
            {
                request.Status = GlobalConstants.RequestStatusTypes.Completed;
                request.ClosedDate = DateTime.UtcNow;
            }
        }

        public void ProcessDecision(string siteId, string approverId, string assetRequestId, string requestId, string decision, string message = "")
        {
            var site = this.SiteService.GetById(siteId);
            var approver = this.Repository.GetAll<PortalUser>().FirstOrDefault(x => x.Id == approverId);
            var request = this.Repository.GetAll<Request>().FirstOrDefault(x => x.Id == requestId);
            var requestor = this.Repository.GetAll<PortalUser>().FirstOrDefault(x => x.Id == request.RequestorId);
            var assetRequest = request.AssetRequests.FirstOrDefault(x => x.Id == assetRequestId);

            ProcessAssetDecision(site, approver, requestor, request, assetRequest, decision, message);
        }

        private void ProcessAssetDecision(Site site, PortalUser approver, PortalUser requestor, Request request, AssetRequestDetail requestDetail, string decision, string message = "")
        {
            IoC.Container.GetInstance<IActivityLogService>().LogActivity(GlobalConstants.ActivityTypes.AssetDecision, site.SiteSettings.PortalTag, approver.FirstName + " " + approver.LastName, request.RequestNum, decision);

            switch (requestDetail.TaskSteps[requestDetail.TaskCurrentStep].Action)
            {
                case "wait-owner":
                case "wait-manager":
                    switch (decision)
                    {
                        case "approved":
                            if (site.SiteSettings.Features.Contains("allowmultiplerequests"))
                            {
                                // Deny any other requests
                                this.DenyOtherPendingRequests(site, requestDetail, approver, request);
                            }

                            this.SetAssetRequestStatus(request, requestDetail.Id, GlobalConstants.RequestStatusTypes.Approved, message);
                            this.TryCloseRequest(request);

                            requestDetail.TaskCurrentStep++;
                            break;

                        case "denied":
                            this.SetAssetRequestStatus(request, requestDetail.Id, GlobalConstants.RequestStatusTypes.Denied, message);
                            this.TryCloseRequest(request);
                            // Show asset
                            // todo: Move to task step
                            var asset = this.AssetService.GetById(requestDetail.Id);
                            asset.IsVisible = true;
                            this.AssetService.Save(asset);
                            requestDetail.TaskCurrentStep = requestDetail.TaskSteps.Count() - 1; // Jump to the last step

                            this.EmailService.SendRequestorNotification(site, requestor, request, requestDetail);

                            break;
                    }

                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].ActionResult = "complete";
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].DatePerformed = DateTime.UtcNow;
                    this.Save(request);

                    // Check for more tasks to process
                    this.ProcessAssetRequest(site, requestor, request, requestDetail);

                    #region Process wish lists
                    switch (decision)
                    {
                        case "approved":
                            var reqs = this.Repository.GetAll<Request>()
                                  .Where(x => (x.Status == GlobalConstants.RequestStatusTypes.Open || x.Status == GlobalConstants.RequestStatusTypes.Pending)
                                  && x.PortalId == request.PortalId && x.AssetRequests.Any(a => a.Id == requestDetail.Id));

                            if (reqs != null && reqs.Count() > 0)
                            {
                                foreach (Request req in reqs)
                                {
                                    // Email-Sending Method
                                    Asset asset = this.Repository.All<Asset>().Where(a => a.Id == requestDetail.Id).FirstOrDefault();
                                    this.EmailService.SendRequestAssetNotAvailable(site, req, asset);
                                }
                            }
                            break;

                        case "denied":
                            // Change the Status of Matched-Asset to 'Matched'
                            var dAsset = this.AssetService.GetById(requestDetail.Id);
                            if (dAsset.IsVisible)
                            {
                                List<WishList> userWishLists = this.WorkContext.S.WishListService.GetUserWishLists(site.Id, requestor.Id);
                                if (userWishLists != null && userWishLists.Count > 0)
                                {
                                    foreach (WishList wishList in userWishLists)
                                    {
                                        MatchedAsset cMatchedAsset = this.WorkContext.S.MatchedAssetService.GetByWishListIDAndAssetID(wishList.Id, dAsset.Id);
                                        if (cMatchedAsset != null)
                                        {
                                            if (cMatchedAsset.Status == MatchedAssetStatusTypes.Requested)
                                            {
                                                cMatchedAsset.Status = MatchedAssetStatusTypes.Matched;
                                                this.WorkContext.S.MatchedAssetService.Save(cMatchedAsset);
                                            }
                                        }                                       
                                    }
                                }
                            }

                           
                            break;
                    }
                    #endregion
                    break;
            }

            // Approving or denying a request can affect category counts
            this.SiteService.UpdateCategories(site.Id); // todo: Logic currently does not take visibility into account
            this.SiteService.UpdateManufacturers(site.Id); // todo: Logic currently does not take visibility into account
        }

        private void DenyOtherPendingRequests(Site site, AssetRequestDetail requestDetail, PortalUser approver, Request winningRequest)
        {
            var requests = this.GetByAssetId(site.Id, requestDetail.Id);

            foreach (var request in requests)
            {
                if (request.Id == winningRequest.Id) continue; // Bail out if this is the winning request

                foreach (var assetRequestDetail in request.AssetRequests.Where(x => x.Id == requestDetail.Id))
                {
                    this.ProcessDecision(site.Id, approver.Id, assetRequestDetail.Id, request.Id, "denied", "Asset awarded to another requestor");
                }
            }
            
        }

 

        public void Process(string siteId, string requestorId, string requestId)
        {
            var site = this.SiteService.GetById(siteId);
            var requestor = this.Repository.GetAll<PortalUser>().FirstOrDefault(x => x.Id == requestorId);
            var request = this.Repository.GetAll<Request>().FirstOrDefault(x => x.Id == requestId);
            request.Status = GlobalConstants.RequestStatusTypes.Pending;
            this.Save(request);

            foreach (var assetRequest in request.AssetRequests)
            {
                this.ProcessAssetRequest(site, requestor, request, assetRequest);
            }


        }

        /// <summary>
        //initiate-request

        //Set request status = pending
        //Set visibility = false
        //Recalculate categories

        //notify-owner

        //Send owner email – pending approval
        //-	Email is sent to user’s manager

        //notify-location-pending

        //Send location email – pending approval
        //-	Email sent to all location owners
        //-	Email is sent to user’s manager

        //notify-manager

        //Send manager email - pending approval
        //-	Email is sent to user’s manager

        //notify-requestor

        //Send requestor email
        //-	If approved:
        //o Email sent to requestor and asset owner
        //-	If denied:
        //o Email is sent to requestor and requestor’s manager

        //notify-others

        //If approved, send requestor email to others – asset approved
        //-	Uses CC list from portal’s settings

        //notify-location

        //Send location email to others – asset approved
        //-	Email is sent to requestor and asset owner

        //complete-request

        //If asset request status = approved, set asset status to approved
        //Recalculate categories
        /// </summary>
        /// <param name="site"></param>
        /// <param name="requestor"></param>
        /// <param name="request"></param>
        /// <param name="requestDetail"></param>
        private void ProcessAssetRequest(Site site, PortalUser requestor, Request request, AssetRequestDetail requestDetail)
        {
            if (requestDetail.TaskCurrentStep >= requestDetail.TaskSteps.Count)
                return; // All done!

            IoC.Container.GetInstance<IActivityLogService>().LogActivity(GlobalConstants.ActivityTypes.ProcessAssetRequest, site.SiteSettings.PortalTag, request.RequestorName, request.RequestNum, requestDetail.TaskSteps[requestDetail.TaskCurrentStep].Action);

            switch (requestDetail.TaskSteps[requestDetail.TaskCurrentStep].Action)
            {
                case "initiate-request":
                    this.SetAssetRequestStatus(request, requestDetail.Id, GlobalConstants.RequestStatusTypes.Pending);

                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].ActionResult = "complete";
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].DatePerformed = DateTime.UtcNow;
                    requestDetail.TaskCurrentStep++;
                    this.Save(request);

                    if (site.SiteSettings.Features.Contains("allowmultiplerequests"))
                    {
                        // Do not hide asset
                    }
                    else
                    {
                        // Hide asset
                        // todo: Move to task step
                        var asset = this.AssetService.GetById(requestDetail.Id);
                        asset.IsVisible = false;
                        this.AssetService.Save(asset);
                    }

                    #region Update wish lists
                    List <WishList> userWishLists = this.WorkContext.S.WishListService.GetUserWishLists(site.Id, requestor.Id);
                    if (userWishLists != null && userWishLists.Count > 0)
                    {
                        var asset = this.AssetService.GetById(requestDetail.Id);
                        foreach (WishList wishList in userWishLists)
                        {
                            MatchedAsset cMatchedAsset = this.WorkContext.S.MatchedAssetService.GetByWishListIDAndAssetID(wishList.Id, asset.Id);
                            if (cMatchedAsset == null)
                            {
                                // Create MatchedAsset-Object
                                string matchedAssetID = this.WorkContext.S.MatchedAssetService.Add(wishList.Id, asset.Id);
                                cMatchedAsset = this.WorkContext.S.MatchedAssetService.GetById(matchedAssetID);
                            }
                            if (cMatchedAsset.Status == MatchedAssetStatusTypes.Matched)
                            {
                                cMatchedAsset.Status = MatchedAssetStatusTypes.Requested;
                                this.WorkContext.S.MatchedAssetService.Save(cMatchedAsset);
                            }
                        }
                    }
                    #endregion

                    this.SiteService.UpdateCategories(site.Id);
                    this.SiteService.UpdateManufacturers(site.Id);

                    // Check for more tasks to process
                    this.ProcessAssetRequest(site, requestor, request, requestDetail);
                    break;

                case "notify-owner":
                    this.EmailService.SendOwnerNotificationPendingApproval(site, requestor, request, requestDetail);
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].ActionResult = "complete";
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].DatePerformed = DateTime.UtcNow;
                    requestDetail.TaskCurrentStep++;
                    this.Save(request);

                    // Check for more tasks to process
                    this.ProcessAssetRequest(site, requestor, request, requestDetail);
                    break;

                case "notify-location-pending":
                    this.EmailService.SendLocationPendingApprovalNotification(site, requestor, request, requestDetail);
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].ActionResult = "complete";
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].DatePerformed = DateTime.UtcNow;
                    requestDetail.TaskCurrentStep++;
                    this.Save(request);

                    // Check for more tasks to process
                    this.ProcessAssetRequest(site, requestor, request, requestDetail);
                    break;

                case "notify-manager":
                    this.EmailService.SendManagerPendingApproval(site, requestor, request, requestDetail);
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].ActionResult = "complete";
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].DatePerformed = DateTime.UtcNow;
                    requestDetail.TaskCurrentStep++;
                    this.Save(request);

                    // Check for more tasks to process
                    this.ProcessAssetRequest(site, requestor, request, requestDetail);
                    break;

                case "notify-requestor":
                    this.EmailService.SendRequestorNotification(site, requestor, request, requestDetail);
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].ActionResult = "complete";
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].DatePerformed = DateTime.UtcNow;
                    requestDetail.TaskCurrentStep++;
                    this.Save(request);

                    // Check for more tasks to process
                    this.ProcessAssetRequest(site, requestor, request, requestDetail);
                    break;

                case "notify-others":
                    this.EmailService.SendRequestorNotificationToOthers(site, requestor, request, requestDetail);
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].ActionResult = "complete";
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].DatePerformed = DateTime.UtcNow;
                    requestDetail.TaskCurrentStep++;
                    this.Save(request);

                    // Check for more tasks to process
                    this.ProcessAssetRequest(site, requestor, request, requestDetail);
                    break;

                case "notify-location":
                    this.EmailService.SendLocationNotificationApproved(site, requestor, request, requestDetail);
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].ActionResult = "complete";
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].DatePerformed = DateTime.UtcNow;
                    requestDetail.TaskCurrentStep++;
                    this.Save(request);

                    // Check for more tasks to process
                    this.ProcessAssetRequest(site, requestor, request, requestDetail);
                    break;

                case "complete-request":
                    // Hide item
                    if (requestDetail.Status == GlobalConstants.RequestStatusTypes.Approved)
                    {
                        var asset = this.AssetService.GetById(requestDetail.Id);
                        this.AssetService.SetAssetStatus(asset, GlobalConstants.AssetStatusTypes.Requested);
                        this.AssetService.Save(asset);
                    }
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].ActionResult = "complete";
                    requestDetail.TaskSteps[requestDetail.TaskCurrentStep].DatePerformed = DateTime.UtcNow;
                    requestDetail.TaskCurrentStep++;
                    this.Save(request);

                    this.SiteService.UpdateCategories(site.Id);
                    this.SiteService.UpdateManufacturers(site.Id);

                    // Check for more tasks to process
                    this.ProcessAssetRequest(site, requestor, request, requestDetail);
                    break;
            }

        }

        private bool ValidateAddress(Address address)
        {
            bool result =
                (string.IsNullOrEmpty(address.Street1) ||
                 string.IsNullOrEmpty(address.City) ||
                 string.IsNullOrEmpty(address.State) ||
                 string.IsNullOrEmpty(address.Zip));

            return !result;
        }

        public void RemoveFromRequest(string siteId, string requestorId, string assetRequestId)
        {
            var request = (from r in this.Repository.GetAll<Request>()
                           where r.PortalId == siteId && r.AssetRequests.Any(ar => ar.Id == assetRequestId)
                           select r).FirstOrDefault();
            var user = this.Repository.GetAll<PortalUser>().FirstOrDefault(x => x.Id == requestorId);
            if ((request != null) && (user != null))
                this.RemoveFromRequest(request, assetRequestId);

            return;
        }

        public void RemoveFromRequest(Request request, string assetRequestId)
        {
            var assetRequest = request.AssetRequests.FirstOrDefault(x => x.Id == assetRequestId);
            var site = this.SiteService.GetById(request.PortalId);
            IoC.Container.GetInstance<IActivityLogService>().LogActivity(GlobalConstants.ActivityTypes.RemoveFromCart, site.SiteSettings.PortalTag, request.RequestorName, request.RequestNum, assetRequest.HitNumber);
            request.AssetRequests.Remove(assetRequest);
            if (request.AssetRequests.Any())
            {
                request.AssetCount = request.AssetRequests.Count;
                this.Save(request);
            }
            else
                this.Delete(request.Id);
        }

        public Request AddToRequest(string siteId, string requestorId, string hitNumber)
        {
            var asset = this.Repository.GetAll<Asset>().FirstOrDefault(x => x.PortalId == siteId && x.HitNumber == hitNumber);
            var user = this.Repository.GetAll<PortalUser>().FirstOrDefault(x => x.Id == requestorId);
            if ((asset != null) && (user != null))
                return this.AddToRequest(asset, user);

            return null;
        }

        /// <summary>
        /// Add an asset to an existing request
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="requestor"></param>
        public Request AddToRequest(Asset asset, PortalUser requestor)
        {
            // Is this asset available for request?
            if (!this.AvailableForRequest(asset))
                return null; //todo: Return an error and show user

            var request = GetOpenOrNewRequest(asset.PortalId, requestor.Id);

            // Is this asset in the request already?
            if (request.AssetRequests.Any(x => x.Id == asset.Id))
                return null;
            request.RequestDate = DateTime.Now;
            var assetDetail = Mapper.Map<Asset, AssetRequestDetail>(asset);

            // Load in the asset manager's info
            var userManager = IoC.Container.GetInstance<PortalUserService>();
            try
            {
                var assetOwner = userManager.FindById(asset.OwnerId);
                assetDetail.OwnerName = assetOwner.FirstName + " " + assetOwner.LastName;
                assetDetail.OwnerEmail = assetOwner.Email;
                assetDetail.OwnerId = assetOwner.Id;
                assetDetail.OwnerPhone = assetOwner.PhoneNumber;

                var site = this.Repository.All<Site>().FirstOrDefault(x => x.Id == asset.PortalId);
                if (site.SiteSettings.AllowSelfSelectedApprovers)
                {
                    request.ApprovingManagerName = requestor.ApprovingManagerName;
                    request.ApprovingManagerEmail = requestor.ApprovingManagerEmail;
                    request.ApprovingManagerId = requestor.ApprovingManagerId;
                    request.ApprovingManagerPhone = requestor.ApprovingManagerPhone;
                }
                else
                {
                    var defaultApprover = this.Repository.All<PortalUser>().FirstOrDefault(x => x.Email == site.SiteSettings.ApprovingManagerEmail);
                    if (defaultApprover != null)
                    {
                        request.ApprovingManagerName = defaultApprover.FirstName + " " + defaultApprover.LastName;
                        request.ApprovingManagerEmail = defaultApprover.Email;
                        request.ApprovingManagerId = defaultApprover.Id;
                        request.ApprovingManagerPhone = defaultApprover.PhoneNumber;
                    }
                }

                assetDetail.TaskCurrentStep = 0;
                // If this site has no specific approval steps, use the default
                if (site.SiteSettings.ApprovalSteps == null)
                {
                    site.SiteSettings.ApprovalSteps = this.SiteService.GetDefaultProcess();
                    this.SiteService.Save(site);
                }
                // Copy the approval steps from the site to the request
                foreach (var step in site.SiteSettings.ApprovalSteps)
                    assetDetail.TaskSteps.Add(new TaskStep() { Action = step.Action, ActionResult = "", TaskType = step.TaskType });

                var location = this.WorkContext.CurrentSite.Locations.FirstOrDefault(x => x.Name == asset.Location);
                if (location != null)
                {
                    assetDetail.AssetAddress = location.Address;
                    assetDetail.LocationName = location.Name;
                    assetDetail.LocationOwnerName = location.OwnerName;
                    assetDetail.LocationOwnerEmail = location.OwnerEmail;
                    assetDetail.LocationOwnerPhone = location.OwnerPhone;
                }

            }
            catch (Exception)
            {

                throw;
            }

            request.AssetRequests.Add(assetDetail);
            request.AssetCount = request.AssetRequests.Count;
            this.Save(request);

            var theSite = this.SiteService.GetById(request.PortalId);
            IoC.Container.GetInstance<IActivityLogService>().LogActivity(GlobalConstants.ActivityTypes.AddToCart, theSite.SiteSettings.PortalTag, requestor.FirstName + " " + requestor.LastName, request.RequestNum, asset.HitNumber);

            return request;
        }

        public RequestIndexModel BuildRequestIndexModel(ISite site, PortalUser portalUser)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<RequestIndexModel>();

            var request = (from r in this.AssetService.Repository.All<Request>()
                           where r.PortalId == site.Id && r.RequestorId == portalUser.Id && r.Status == GlobalConstants.RequestStatusTypes.Open
                           select r).FirstOrDefault();

            if (request == null)
                request = GetOpenOrNewRequest(site.Id, portalUser.Id);

            if (site.SiteSettings.AllowSelfSelectedApprovers)
            {
                // Nothing to do, form will display a popup
            }
            else
            {
                var defaultApprover = this.Repository.All<PortalUser>().FirstOrDefault(x => x.Email == site.SiteSettings.ApprovingManagerEmail);
                if (defaultApprover != null)
                {
                    request.ApprovingManagerName = defaultApprover.FirstName + " " + defaultApprover.LastName;
                    request.ApprovingManagerEmail = defaultApprover.Email;
                    request.ApprovingManagerId = defaultApprover.Id;
                    request.ApprovingManagerPhone = defaultApprover.PhoneNumber;

                    this.UpdateManager(request.Id, request.ApprovingManagerId, request.ApprovingManagerEmail, request.ApprovingManagerName, request.ApprovingManagerPhone);

                    this.Save(request);
                }
            }

            if (request != null && request.AssetRequests != null && request.AssetRequests.Count > 0)
            {
                IList<AssetRequestDetail> approvedAssets = new List<AssetRequestDetail>();

                foreach (AssetRequestDetail requestAsset in request.AssetRequests)
                {
                    Asset cAsset = this.Repository.All<Asset>().Where(a => a.Id == requestAsset.Id && a.Status == GlobalConstants.AssetStatusTypes.Requested).FirstOrDefault();
                    if (cAsset != null)
                    {
                        approvedAssets.Add(requestAsset);
                    }
                }

                foreach (AssetRequestDetail removeAsset in approvedAssets)
                {
                    request.AssetRequests.Remove(removeAsset);
                    request.AssetCount--;
                }

                if (request.AssetRequests.Count <= 0)
                {
                    request.Status = GlobalConstants.RequestStatusTypes.Completed;
                }
                this.Save(request);
            }


            model.Request = request;

            model.JsonData = JsonConvert.SerializeObject(model);

            return model;
        }


        private bool AvailableForRequest(Asset asset)
        {
            var result = false;

            if ((asset.AvailForRedeploy <= DateTime.UtcNow) && (asset.AvailForSale > DateTime.UtcNow))
                result = true;

            if (asset.Status == GlobalConstants.AssetStatusTypes.Available)
                result = true;

            return result;
        }

        public AllRequestsReportDataModel BuildAllRequestsReportModel(string siteId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AllRequestsReportDataModel>();

            return model;
        }

        public IList<AllRequestsReportLineItemModel> BuildAllRequestsReportDataModel(string siteId)
        {
            var requests = (from a in this.Repository.All<Request>()
                where a.PortalId == siteId && a.AssetCount > 0 && a.Status != GlobalConstants.RequestStatusTypes.Open
                orderby a.RequestDate descending
                            select a).ToList();

            var baseUrl = this.WorkContext.HttpContext.Request.Url.Authority;
            var controllerUrl = "/" + this.WorkContext.PortalTag + "/asset/index/";
            var baseAssetURL = "https://" + baseUrl + controllerUrl;

            var allRequestData = new List<AllRequestsReportLineItemModel>();
            foreach (Request request in requests)
            {
                foreach (var detail in request.AssetRequests)
                {
                    var requestData = new AllRequestsReportLineItemModel()
                    {
                        Location = detail.LocationName,
                        Status = request.Status.ToString(),
                        RequestorName = request.RequestorName,
                        RequestorPhone = request.RequestorPhone,
                        RequestorEmail = request.RequestorEmail,
                        RequestNum = request.RequestNum,
                        AssetCount = request.AssetCount,

                        Notes = request.Notes,
                        HitNumbers = detail.HitNumber,
                        AssetStatus = detail.Status.ToString(),
                        ViewUrl = "=HYPERLINK(\"" + baseAssetURL + detail.HitNumber + "\", \"Click to View\")",
                        Title = detail.Title,
                        Description = detail.Description,
                        Manufacturer = detail.Manufacturer,
                        ModelNumber = detail.ModelNumber,
                        SerialNumber = detail.SerialNumber,
                        NetBookValues = detail.FormattedBookValue,
                        RequestDate = request.RequestDate,

                        Street1 = request.ShipToAddress.Street1,
                        Street2 = request.ShipToAddress.Street2,
                        City = request.ShipToAddress.City,
                        State = request.ShipToAddress.State,
                        Zip = request.ShipToAddress.Zip,
                        Country = request.ShipToAddress.Country,
                        Attention = request.ShipToAddress.Attention,
                        ShippingNote = request.ShipToAddress.Notes,
                    };

                    //if (requestData.CustomData == "[]")
                    //    requestData.CustomData = "";

                    allRequestData.Add(requestData);
                }
            }

            return allRequestData;
        }

        public void UpdateManager(string requestId, string managerId, string managerEmail, string managerName, string managerPhone)
        {
            var request = this.GetById(requestId);
            if (request == null)
                return;

            var userManager = this.WorkContext.HttpContext.GetOwinContext().GetUserManager<PortalUserService>();
            var requestor = userManager.FindById(request.RequestorId);
            if (requestor == null)
                return;

            if (requestor.Id == managerId)
                return; // Do not allow people to select themselves as a manager

            // Do we have an existing manager?
            if (string.IsNullOrEmpty(managerId))
            {
                if (!string.IsNullOrEmpty(managerEmail))
                {
                    var manager = this.Repository.GetAll<PortalUser>().FirstOrDefault(x => x.Email.ToLower() == managerEmail.ToLower());
                    if (manager != null)
                    {
                        request.ApprovingManagerEmail = manager.Email;
                        request.ApprovingManagerName = manager.FirstName + " " + manager.LastName;
                        request.ApprovingManagerPhone = manager.PhoneNumber;
                        request.ApprovingManagerId = manager.Id;
                    }
                    else
                    {
                        // Save info so we can ask the manager to create an account
                        request.ApprovingManagerEmail = managerEmail;
                        request.ApprovingManagerName = managerName;
                        request.ApprovingManagerPhone = managerPhone;
                        request.ApprovingManagerId = null; // Manager does yet yet have an account
                    }
                    this.Save(request);

                    // Save approver info back into user record to use a default for the next request
                    requestor.ApprovingManagerName = request.ApprovingManagerName;
                    requestor.ApprovingManagerEmail = request.ApprovingManagerEmail;
                    requestor.ApprovingManagerPhone = request.ApprovingManagerPhone;
                    requestor.ApprovingManagerId = request.ApprovingManagerId;
                    userManager.UpdateAsync(requestor);
                }
            }
            else
            {
                var manager = this.Repository.GetAll<PortalUser>().FirstOrDefault(x => x.Id == managerId);
                if (manager != null)
                {
                    request.ApprovingManagerPhone = manager.PhoneNumber;
                    request.ApprovingManagerEmail = manager.Email;
                    request.ApprovingManagerName = manager.FirstName + " " + manager.LastName;
                    request.ApprovingManagerId = manager.Id;
                    this.Save(request);

                    // Save approver info back into user record to use a default for the next request
                    requestor.ApprovingManagerName = request.ApprovingManagerName;
                    requestor.ApprovingManagerEmail = request.ApprovingManagerEmail;
                    requestor.ApprovingManagerPhone = request.ApprovingManagerPhone;
                    requestor.ApprovingManagerId = request.ApprovingManagerId;
                    userManager.UpdateAsync(requestor);
                }
            }
        }

        public void SendReminder(string siteId, string userId, string requestId)
        {
            var request = this.GetById(requestId);
            if (request == null)
                return;

            var site = this.SiteService.GetById(siteId);
            var userManager = this.WorkContext.HttpContext.GetOwinContext().GetUserManager<PortalUserService>();
            var requestor = userManager.FindById(userId);
            this.EmailService.SendManagerPendingApproval(site, requestor, request);
        }

        public void UpdateNotes(string requestId, string notes)
        {
            var request = this.GetById(requestId);
            if (request == null)
                return;

            request.Notes = notes;
            this.Save(request);
        }

        public List<Request> GetPendingRequests(int waitingDays, Site site)
        {
            List<Request> pendingRequests = new List<Request>();
            try
            {
                DateTime reminderStart = DateTime.Now.AddDays(waitingDays);

                IQueryable<Request> requestList = from r in this.Repository.All<Request>()
                                                  where r.PortalId == site.Id
                                                  && r.RequestDate <= reminderStart
                                                  && r.Status == GlobalConstants.RequestStatusTypes.Pending
                                                  orderby r.RequestNum descending
                                                  select r;

                pendingRequests = requestList.ToList();

            }
            catch (Exception ex)
            {
                throw;
            }
            return pendingRequests;
        }

        public List<Request> GetByAssetId(string siteId, string assetId)
        {
            IQueryable<Request> requestList = from r in this.Repository.All<Request>()
                where r.PortalId == siteId
                      && r.AssetRequests.Any(x => ((x.Id == assetId) && (x.Status == RequestStatusTypes.Pending)))
                      && r.Status == GlobalConstants.RequestStatusTypes.Pending
                orderby r.RequestNum descending
                select r;

            return requestList.ToList();
        }

        public IAssetService AssetService { get; set; }

        public IEmailService EmailService { get; set; }
 }


    class RequestEqualityComparer : IEqualityComparer<Request>
    {
        public bool Equals(Request x, Request y)
        {
            // Two items are equal if their keys are equal.
            return x.Id == y.Id;
        }

        public int GetHashCode(Request obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}