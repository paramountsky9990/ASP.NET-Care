using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web;
using AutoMapper;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;

namespace HGP.Web.Services
{
    public interface IPortalServices
    {
        IAssetService AssetService { get; set; }
        IAwsService AwsService { get; set; }
        IDraftAssetService DraftAssetService { get; set; }
        IDraftAssetInboxService DraftAssetInboxService { get; set; }
        IInBoxService InBoxService { get; set; }
        IInBoxItemService InBoxItemService { get; set; }
        IHeaderService HeaderService { get; set; }
        IListService ListService { get; set; }
        IEmailService EmailService { get; set; }
        IRequestService RequestService { get; set; }
        ISiteService SiteService { get; set; }
        IWishListService WishListService { get; set; }
        IMatchedAssetService MatchedAssetService { get; set; }
        IUnsubscribeService UnsubscribeService { get; set; }

        IWorkContext WorkContext { get; }
    }

    public class PortalServices : IPortalServices
    {
        public PortalServices(IAssetService assetService, IAwsService awsService, IEmailService emailService, IDraftAssetService draftAssetService, IDraftAssetInboxService draftAssetInboxService, IHeaderService headerService,
            IInBoxService inBoxService, IInBoxItemService inBoxItemService, IListService listService, IRequestService requestService, IWishListService wishListService,
                               IMatchedAssetService matchedAssetService, IUnsubscribeService unsubscribeService, ISiteService siteService,
                               IWorkContext workContext)
        {
            Contract.Requires(assetService != null);
            Contract.Requires(awsService != null);
            Contract.Requires(emailService != null);
            Contract.Requires(draftAssetService != null);
            Contract.Requires(draftAssetService != null);
            Contract.Requires(headerService != null);
            Contract.Requires(inBoxService != null);
            Contract.Requires(inBoxItemService != null);
            Contract.Requires(listService != null);
            Contract.Requires(requestService != null);
            Contract.Requires(siteService != null);
            Contract.Requires(wishListService != null);
            Contract.Requires(matchedAssetService != null);
            Contract.Requires(unsubscribeService != null);

            Contract.Requires(workContext != null);

            this.AssetService = assetService;
            this.AwsService = awsService;
            this.DraftAssetService = draftAssetService;
            this.DraftAssetInboxService = draftAssetInboxService;
            this.HeaderService = headerService;
            this.EmailService = emailService;
            this.InBoxService = inBoxService;
            this.InBoxItemService = inBoxItemService;
            this.ListService = listService;
            this.RequestService = requestService;
            this.SiteService = siteService;
            this.WishListService = wishListService;
            this.MatchedAssetService = matchedAssetService;
            this.UnsubscribeService = unsubscribeService;

            this.WorkContext = workContext;

        }



        public IAssetService AssetService { get; set; }
        public IAwsService AwsService { get; set; }
        public IEmailService EmailService { get; set; }
        public IDraftAssetService DraftAssetService { get; set; }
        public IDraftAssetInboxService DraftAssetInboxService { get; set; }
        public IHeaderService HeaderService { get; set; }
        public IInBoxService InBoxService { get; set; }
        public IInBoxItemService InBoxItemService { get; set; }
        public IListService ListService { get; set; }
        public PortalUserService PortalUserService { get; set; }
        public IRequestService RequestService { get; set; }
        public ISiteService SiteService { get; set; }
        public IWishListService WishListService { get; set; }
        public IMatchedAssetService MatchedAssetService { get; set; }
        public IUnsubscribeService UnsubscribeService { get;  set; }

        public IWorkContext WorkContext { get; private set; }


    }
}