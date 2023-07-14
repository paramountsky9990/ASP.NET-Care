using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using HGP.Common;
using HGP.Web.DependencyResolution;
using HGP.Web.Models;
using HGP.Web.Models.Assets;
using HGP.Web.Models.Drafts;
using HGP.Web.Models.InBox;
using Microsoft.AspNet.Identity;

namespace HGP.Web.Services
{
    public interface IDraftAssetInboxService : IBaseService
    {
        void Save(DraftAssetInboxItem entry);
        void Delete(string id);
        DraftAssetInboxItem GetById(string id);
        DraftAssetInboxItem GetByHitNumber(string siteId, string hitNumber);
        int GetInBoxCountByType(string siteId, string userId, GlobalConstants.InboxItemTypes[] inboxTypes);
    }


    public class DraftAssetInboxService : BaseService<DraftAssetInboxItem>, IDraftAssetInboxService
    {
        public DraftsIndexModel BuildDraftsHomeModel(string currentSiteId, string currentUserId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<DraftsIndexModel>();

            var draftAssets = (from a in this.Repository.All<DraftAsset>()
                where a.PortalId == currentSiteId && a.OwnerId == currentUserId
                orderby a.UpdatedDate descending
                select a).ToList();

            model.DraftAssets = draftAssets;

            return model;
        }

        public DraftAssetInboxItem GetByHitNumber(string siteId, string hitNumber)
        {
            var inboxItem = (from i in Repository.All<DraftAssetInboxItem>()
                where i.PortalId == siteId && i.DraftAssetHitNumber == hitNumber
                select i).FirstOrDefault();

            return inboxItem;
        }

        public int GetInBoxCountByType(string siteId, string userId, GlobalConstants.InboxItemTypes[] inboxTypes)
        {
            var count = 0;
            var userManager = IoC.Container.GetInstance<PortalUserService>();
            var user = userManager.FindById(userId);
            if (user != null && (user.Roles.Contains("ClientAdmin") || user.Roles.Contains("SuperAdmin")))
            {
                // Find requests for any approvers in this portal
                count = (from r in this.Repository.All<DraftAssetInboxItem>()
                    where r.PortalId == siteId && inboxTypes.Contains(r.Type) && r.Status == GlobalConstants.InboxStatusTypes.Pending
                    select r).Count();
            }

            return count;
        }

    }
}