using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper.Internal;
using Glimpse.Core.Extensions;
using HGP.Common;
using HGP.Web.DependencyResolution;
using HGP.Web.Models;
using HGP.Web.Models.Assets;
using Newtonsoft.Json;

namespace HGP.Web.Services
{
    public interface IHeaderService
    {
        HeaderModel BuildHeaderModel(string siteId, string userId);
        HeaderModel BuildHeaderModel(string siteId);
        HeaderModel BuildHeaderModel();
    }

    public class HeaderService : IHeaderService
    {
        public HeaderService(IInBoxService inBoxService, IRequestService requestService, ISiteService siteService, IDraftAssetInboxService draftAssetInboxService)
        {
            this.InBoxService = inBoxService;
            this.RequestService = requestService;
            this.SiteService = siteService;
            this.DraftAssetInboxService = draftAssetInboxService;
        }


        /// <summary>
        /// Build headder model for a logged in user
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public HeaderModel BuildHeaderModel(string siteId, string userId)
        {
            var model = new HeaderModel();
            var site = this.SiteService.GetById(siteId);
            if (site.Categories != null)
            {
                var cats = site.Categories.OrderByDescending(x => x.Count).ToList();

                var rootCategory = new Category()
                {
                    Name = "All",
                    Count = cats.Sum(x => x.Count),
                    UriString = "",
                };

                model.Categories = new List<Category>();
                model.Categories.Insert(0, rootCategory);
                foreach (var aCat in cats.Take(10).OrderBy(x => x.Name))
                {
                    model.Categories.Add(aCat);
                }
            }
            var context = IoC.Container.GetInstance<HttpContext>();
            if ((context != null) && (context.Request.UrlReferrer != null)) 
                model.ReferringPage = context.Request.UrlReferrer.PathAndQuery;
            model.PortalTag = site.SiteSettings.PortalTag;
            model.Request = this.RequestService.GetOpenOrNewRequest(siteId, userId);
            model.InBoxCount = this.InBoxService.GetInBoxCount(siteId, userId);
            model.TransfersCount = this.RequestService.GetRequestCountByStatus(siteId, userId, new[] { GlobalConstants.RequestStatusTypes.Pending, GlobalConstants.RequestStatusTypes.Completed, GlobalConstants.RequestStatusTypes.Denied, GlobalConstants.RequestStatusTypes.Approved });
            model.PendingTransfersCount = this.RequestService.GetRequestCountByStatus(siteId, userId, new [] { GlobalConstants.RequestStatusTypes.Pending });
            if (site.SiteSettings.Features.Contains("selfupload"))
                model.PendingDraftAssetsCount = this.DraftAssetInboxService.GetInBoxCountByType(siteId, userId, new[] {GlobalConstants.InboxItemTypes.DraftAssetApprovalDecision});
            else
                model.PendingDraftAssetsCount = 0;
            model.ClosedTransfersCount = this.RequestService.GetRequestCountByStatus(siteId, userId, new [] { GlobalConstants.RequestStatusTypes.Completed, GlobalConstants.RequestStatusTypes.Denied, GlobalConstants.RequestStatusTypes.Approved });
            model.JsonData = JsonConvert.SerializeObject(model);
            return model;
        }

        /// <summary>
        /// Build model for the header without a a site or a user
        /// </summary>
        /// <param name="siteId"></param>
        /// <returns></returns>
        public HeaderModel BuildHeaderModel()
        {
            var model = new HeaderModel();
            var context = IoC.Container.GetInstance<HttpContext>();
            if ((context != null) && (context.Request.UrlReferrer != null))
                model.ReferringPage = context.Request.UrlReferrer.PathAndQuery;
            model.PortalTag = "";
            model.Request = new Request();
            model.JsonData = JsonConvert.SerializeObject(model);
            return model;
        }

        /// <summary>
        /// Build model for the header without a user logged in
        /// </summary>
        /// <param name="siteId"></param>
        /// <returns></returns>
        public HeaderModel BuildHeaderModel(string siteId)
        {
            var model = new HeaderModel();
            var site = this.SiteService.GetById(siteId);
            var context = IoC.Container.GetInstance<HttpContext>();
            if ((context != null) && (context.Request.UrlReferrer != null)) 
                model.ReferringPage = context.Request.UrlReferrer.PathAndQuery;
            model.PortalTag = site.SiteSettings.PortalTag;
            model.Request = new Request();
            model.JsonData = JsonConvert.SerializeObject(model);
            return model;
        }
        public IRequestService RequestService { get; set; }

        public ISiteService SiteService { get; set; }

        public IInBoxService InBoxService { get; set; }
        public IDraftAssetInboxService DraftAssetInboxService { get; set; }
    }


}