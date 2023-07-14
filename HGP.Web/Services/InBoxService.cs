using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;
using HGP.Web.DependencyResolution;
using HGP.Web.Models;
using HGP.Web.Models.InBox;
using HGP.Common.Database;

namespace HGP.Web.Services
{
    public interface IInBoxService : IBaseService
    {
        IMongoRepository Repository { get; }

        InBoxHomeModel BuildInBoxHomeModel(string siteId, string userId);
        int GetInBoxCount(string siteId, string userId);
    }
    public class InBoxService : BaseService<Request>, IInBoxService
    {
        public InBoxService(IRequestService requestService)
        {
            this.RequestService = requestService;
        }
        public InBoxHomeModel BuildInBoxHomeModel(string siteId, string userId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<InBoxHomeModel>();
            // Calculate how many in box items the user has

            // Count requests awaiting approval
            var requests = this.RequestService.GetByApprovingManagerStatus(siteId, userId,
                                GlobalConstants.RequestStatusTypes.Pending).OrderByDescending(x => x.RequestDate);

            foreach (var request in requests)
            {
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

                }
                if (request.AssetRequests.Count <= 0)
                {
                    request.Status = GlobalConstants.RequestStatusTypes.Completed;
                }
                this.Save(request);

                if (request.Status != GlobalConstants.RequestStatusTypes.Completed)
                {
                    var inboxItem = new InboxItem();
                    inboxItem.OwnerId = userId;
                    inboxItem.PortalId = siteId;
                    inboxItem.Status = GlobalConstants.InboxStatusTypes.Pending;
                    inboxItem.Type = GlobalConstants.InboxItemTypes.RequestApprovalDecision;
                    inboxItem.Data = request;
                    model.RequestItems.Add(inboxItem);
                }
            }

            // Calculate draft assets waiting for approval
            model.DraftAssetItems = (from a in Repository.All<DraftAssetInboxItem>()
                where a.PortalId == siteId && a.Status == GlobalConstants.InboxStatusTypes.Pending
                      orderby a.CreatedDate descending 
                select a).ToList();

            foreach (var draftAssetItem in model.DraftAssetItems)
            {
                draftAssetItem.DraftAsset = this.WorkContext.S.DraftAssetService.GetByHitNumber(siteId, draftAssetItem.DraftAssetHitNumber);
            }

            model.InBoxCount = model.RequestItems.Count + model.DraftAssetItems.Count;

            return model;
        }

        public int GetInBoxCount(string siteId, string userId)
        {
            //todo: Count() should not return all data, just a count, verify by looking at the Mongo log
            var requestCount = this.RequestService.GetByApprovingManagerStatus(siteId, userId,
                                GlobalConstants.RequestStatusTypes.Pending).OrderByDescending(x => x.RequestDate).Count();
            return requestCount;
        }

        public IRequestService RequestService { get; set; }
    }
}