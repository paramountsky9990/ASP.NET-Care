using AutoMapper;
using HGP.Common.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.List;
using HGP.Web.Models.WishLists;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using HGP.Web.Extensions;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Report;
using static HGP.Common.GlobalConstants;

namespace HGP.Web.Services
{
    public interface IWishListService : IBaseService
    {
        void Save(WishList entry);
        WishList GetById(string id);

        List<WishList> GetUserWishLists(string siteID, string portalUserID);
        List<WishList> GetSoonToBeExpiring(string siteID, string portalUserID, int ExpirationReminderOn);

        bool IsWishListExists(string searchCriteria);
        bool AddWishList(string searchCriteria);
        bool EditWishList(string wishListID, string searchCriteria);
        bool IsWishListExtended(string wishListID);
        bool ExtendWishList(string wishListID);
        bool IgnoreMatchedAsset(string wishListID, string assetID);
        string GetWishListByAssetID(string siteID, string portalUserID, string assetID);
        bool CloseWishList(string wishListID);
        bool EditSendMail(string wishListID, bool sendMail);

        // Soft-Delete WishList i.e. Change the Status of the WishList Object : WishListStatusTypes.Removed
        bool DeleteWishList(string wishListID);

        WishListHomeModel BuildWishListHomeModel(string siteID, string portalUserID);
        WishListResultHomeModel BuildWishListResultHomeModel(string siteID, string wishListID);

        IList<AllWishesLineItemModel> BuildAllWishesReportDataModel(string siteId);
        AllWishesReportDataModel BuildAllWishesReportModel(string siteId);
    }

    public class WishListServiceMappingProfile : Profile
    {
        public WishListServiceMappingProfile()
        {
            CreateMap<MediaFileDto, AdminAssetsHomeGridMediaModel>();
            CreateMap<Asset, ListHomeGridModel>();
        }
    }
    public class WishListService : BaseService<WishList>, IWishListService
    {
        public IAssetService AssetService { get; set; }
        public ISiteService SiteService { get; set; }
        public IMatchedAssetService MatchedAssetService { get; set; }
        public IRequestService RequestService { get; set; }

        public WishListService(IMongoRepository repository, IWorkContext workContext,
                                IAssetService assetService, ISiteService siteService,
                                IMatchedAssetService matchedAssetService, IRequestService requestService)
            : base(repository, workContext)
        {
            this.AssetService = assetService;
            this.SiteService = siteService;
            this.MatchedAssetService = matchedAssetService;
            this.RequestService = requestService;
        }


        public bool AddWishList(string searchCriteria)
        {
            bool res = false;
            try
            {
                int expireAfter = Convert.ToInt32(WebConfigurationManager.AppSettings["WishListExpiresAfter"].ToString());
                WishList wishList = new WishList()
                {
                    PortalId = this.WorkContext.CurrentSite.Id,
                    PortalUserId = this.WorkContext.CurrentUser.Id,
                    FirstName = this.WorkContext.CurrentUser.FirstName,
                    LastName = this.WorkContext.CurrentUser.LastName,
                    ExpireOn = DateTime.Now.AddDays(expireAfter),
                    SearchCriteria = searchCriteria,
                    Status = WishListStatusTypes.Open,
                    SendMail = true
                };

                this.Save(wishList);
                res = true;
            }
            catch (Exception ex) { throw; }

            return res;
        }

        public bool IsWishListExists(string searchCriteria)
        {
            bool res = false;
            try
            {
                WishList wishList = this.Repository.All<WishList>().Where(w =>
                   w.PortalId == this.WorkContext.CurrentSite.Id
                    && w.PortalUserId == this.WorkContext.CurrentUser.Id
                    && w.SearchCriteria.ToLower() == searchCriteria.Trim().ToLower()
                    && w.Status == WishListStatusTypes.Open
                     ).FirstOrDefault();
                if (wishList != null)
                {
                    res = true;
                }
            }
            catch (Exception ex) { throw; }

            return res;
        }

        // Get All Active(Open) WishLists of a User
        public List<WishList> GetUserWishLists(string siteID, string portalUserID)
        {
            List<WishList> userWishLists = new List<WishList>();

            try
            {
                var userWishListsQ = this.Repository.All<WishList>().Where(w =>
                    w.PortalId == siteID
                    && w.PortalUserId == portalUserID
                    && w.Status == WishListStatusTypes.Open
                ).OrderByDescending(x => x.CreatedDate);

                userWishLists = userWishListsQ.ToList();

                if (userWishListsQ != null && userWishListsQ.Count() > 0)
                {
                    foreach (WishList wishList in userWishListsQ)
                    {
                        // Expire WishLIst if it matures to its Expired-Date or has exceeded that,
                        // WishLIst is Matured after 90 Days (WishListExpiresAfter) ByDefault,
                        // WishList can be extended to 90 more days (WishListExtendedFor), before it is marked as Expired.
                        if (wishList.ExpireOn.Date <= DateTime.Now.Date)
                        {
                            wishList.Status = WishListStatusTypes.Expired;
                            this.Save(wishList);

                            userWishLists.Remove(wishList);
                        }
                    }
                }
            }
            catch (Exception ex) { throw; }
            return userWishLists;
        }

        public WishListHomeModel BuildWishListHomeModel(string siteID, string portalUserID)
        {
            WishListHomeModel model = new WishListHomeModel();
            try
            {
                var site = IoC.Container.GetInstance<ISiteService>().GetById(siteID);
                if (site == null)
                    return null;

                model = IoC.Container.GetInstance<ModelFactory>().GetModel<WishListHomeModel>();

                List<WishList> userWishLists = this.GetUserWishLists(siteID, portalUserID);

                model.WishListDetails = new List<WishListDetails>();

                foreach (WishList wishList in userWishLists)
                {
                    bool IsEmailSent = false;
                    List<MatchedAsset> matchedAssets = this.MatchedAssetService.GetAllByWishListID(wishList.Id).Where(w => w.Status == MatchedAssetStatusTypes.Matched).ToList();
                    if (matchedAssets.Count > 0)
                    {
                        IsEmailSent = matchedAssets.All(w => w.IsEmailSent == true);
                    }

                    model.WishListDetails.Add(
                        new WishListDetails
                        {
                            WishList = wishList,
                            IsEmailSent = IsEmailSent
                        });
                }

                model.JsonData = JsonConvert.SerializeObject(model);
            }
            catch (Exception ex) { }
            return model;
        }

        public bool EditWishList(string wishListID, string searchCriteria)
        {
            bool res = false;
            try
            {
                WishList wishListObj = this.Repository.All<WishList>(
                       ).FirstOrDefault(w => w.PortalId == this.WorkContext.CurrentSite.Id
                      && w.PortalUserId == this.WorkContext.CurrentUser.Id
                      && w.Id == wishListID
                      && w.Status == WishListStatusTypes.Open);

                if (wishListObj != null)
                {
                    wishListObj.SearchCriteria = searchCriteria;
                    this.Save(wishListObj);
                    res = true;
                }
            }
            catch (Exception ex) { throw; }
            return res;
        }

        public bool DeleteWishList(string wishListID)
        {
            bool res = false;
            try
            {
                WishList wishList = this.Repository.All<WishList>(
                    ).FirstOrDefault(w => w.PortalId == this.WorkContext.CurrentSite.Id &&
                   w.PortalUserId == this.WorkContext.CurrentUser.Id
                   && w.Id == wishListID
                    && w.Status == WishListStatusTypes.Open);
                if (wishList != null)
                {
                    wishList.Status = WishListStatusTypes.Removed;
                    this.Save(wishList);
                    res = true;
                }
            }
            catch (Exception ex) { throw; }
            return res;
        }

        public bool IsWishListExtended(string wishListID)
        {
            bool res = false;
            try
            {
                WishList wishList = this.Repository.All<WishList>(
                    ).FirstOrDefault(w => w.PortalId == this.WorkContext.CurrentSite.Id &&
                    w.PortalUserId == this.WorkContext.CurrentUser.Id
                    && w.Id == wishListID
                     && w.Status == WishListStatusTypes.Open);
                if (wishList != null)
                {
                    // res = wishList.IsExtended;
                }
            }
            catch (Exception ex) { throw; }
            return res;
        }

        public bool ExtendWishList(string wishListID)
        {
            bool res = false;
            try
            {
                int extendedFor = Convert.ToInt32(WebConfigurationManager.AppSettings["WishListExtendedFor"].ToString());
                WishList wishList = this.GetById(wishListID);

                if (wishList != null)
                {
                    wishList.ExpireOn = wishList.ExpireOn.AddDays(extendedFor);
                    this.Save(wishList);
                    res = true;
                }
            }
            catch (Exception ex) { throw; }
            return res;
        }

        public WishListResultHomeModel BuildWishListResultHomeModel(string siteID, string wishListID)
        {
            WishListResultHomeModel model = new WishListResultHomeModel();
            try
            {
                var site = IoC.Container.GetInstance<ISiteService>().GetById(siteID);
                if (site == null)
                    return null;

                model = IoC.Container.GetInstance<ModelFactory>().GetModel<WishListResultHomeModel>();

                WishList cWishList = this.GetById(wishListID);
                var utcNow = DateTime.UtcNow;

                if (cWishList != null)
                {
                    model.WishList = cWishList;

                    // Build a list of all pending assets so they can be hidden
                    var requestedAssetIds = this.RequestService.GetRequestedAssetIds(site.Id);

                    IQueryable<Asset> wishListAssets = this.AssetService.Repository.TextSearch<Asset>(cWishList.SearchCriteria, site.Id)
                        .Where(a => a.IsVisible == true && (a.AvailForSale > utcNow) && !requestedAssetIds.Contains(a.Id))
                        .OrderByDescending(x => x.CreatedDate)
                        .AsQueryable();


                    if (wishListAssets.Count() > 0)
                    {
                        // Grab the data and map
                        var mappedAssets = Mapper.Map<List<Asset>, List<ListHomeGridModel>>(wishListAssets.ToList());

                        List<ListHomeGridModel> wishListMatchedAssets = new List<ListHomeGridModel>();

                        foreach (var asset in mappedAssets)
                        {
                            MatchedAsset matchedAsset = this.MatchedAssetService.GetByWishListIDAndAssetID(cWishList.Id, asset.Id);
                            if (matchedAsset == null)
                            {
                                //Create MatchedAsset Object
                                string matchedAssetID = this.MatchedAssetService.Add(cWishList.Id, asset.Id);
                                matchedAsset = this.MatchedAssetService.GetById(matchedAssetID);
                            }

                            var timeSpan = asset.AvailForSale - utcNow;
                            asset.MinutesRemaining = timeSpan.TotalMinutes;
                            if (matchedAsset != null)
                            {
                                if (matchedAsset.Status == MatchedAssetStatusTypes.Matched)
                                {
                                    wishListMatchedAssets.Add(asset);
                                }
                            }
                        }

                        model.Assets = wishListMatchedAssets;
                    }
                }

                model.JsonData = JsonConvert.SerializeObject(model);

            }
            catch (Exception ex) { throw; }
            return model;
        }

        public bool IgnoreMatchedAsset(string wishListID, string assetID)
        {
            bool res = false;
            try
            {
                MatchedAsset matchedAsset = this.MatchedAssetService.GetByWishListIDAndAssetID(wishListID, assetID);
                if (matchedAsset != null)
                {
                    // Change status to Ignore
                    matchedAsset.Status = MatchedAssetStatusTypes.Ignored;
                    this.MatchedAssetService.Save(matchedAsset);
                    res = true;
                }
            }
            catch (Exception ex) { throw; }
            return res;
        }

        public List<WishList> GetSoonToBeExpiring(string siteID, string portalUserID, int ExpirationReminderOn)
        {
            List<WishList> expiringWishLists = new List<WishList>();

            try
            {
                DateTime reminderStart = DateTime.Now.AddDays(ExpirationReminderOn);

                IQueryable<WishList> userWishListsQ = this.Repository.All<WishList>().Where(w =>
                w.PortalId == siteID
                && w.PortalUserId == portalUserID
                && w.Status == WishListStatusTypes.Open);
                if (userWishListsQ != null && userWishListsQ.Count() > 0)
                {
                    foreach (WishList wishlist in userWishListsQ)
                    {
                        if (wishlist.ExpireOn.Date == reminderStart.Date)
                        {
                            expiringWishLists.Add(wishlist);
                        }
                    }
                }
            }
            catch (Exception ex) { throw; }
            return expiringWishLists;
        }

        public AllWishesReportDataModel BuildAllWishesReportModel(string siteId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AllWishesReportDataModel>();

            return model;
        }
        public IList<AllWishesLineItemModel> BuildAllWishesReportDataModel(string siteId)
        {
            var wishes = (from a in this.Repository.All<WishList>()
                          where a.PortalId == siteId
                          orderby a.CreatedDate descending
                          select a).ToModelList<AllWishesLineItemModel>();

            return wishes.ToModelList<AllWishesLineItemModel>();
        }

        public string GetWishListByAssetID(string siteID, string portalUserID, string assetID)
        {
            string wishListID = string.Empty;
            try
            {
                Asset cAsset = this.AssetService.GetById(assetID);
                var ClientIdNumber = cAsset.ClientIdNumber;
                var HitNumber = cAsset.HitNumber;
                var Manufacturer = cAsset.Manufacturer;
                var ModelNumber = cAsset.ModelNumber;
                var SerialNumber = cAsset.SerialNumber;
                var Title = cAsset.Title;

                if (cAsset != null)
                {
                    List<WishList> userWishLists = this.GetUserWishLists(siteID, portalUserID);
                    if (userWishLists != null && userWishLists.Count > 0)
                    {
                        foreach (WishList wishlist in userWishLists)
                        {
                            if (cAsset.ClientIdNumber.ToLower().Contains(wishlist.SearchCriteria.ToLower())
                                || cAsset.HitNumber.ToLower().Contains(wishlist.SearchCriteria.ToLower())
                                || cAsset.Manufacturer.ToLower().Contains(wishlist.SearchCriteria.ToLower())
                                || cAsset.ModelNumber.ToLower().Contains(wishlist.SearchCriteria.ToLower())
                                || cAsset.SerialNumber.ToLower().Contains(wishlist.SearchCriteria.ToLower())
                                || cAsset.Title.ToLower().Contains(wishlist.SearchCriteria.ToLower())
                                )
                            {
                                return wishlist.Id;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { throw; }
            return wishListID;
        }

        public bool CloseWishList(string wishListID)
        {
            bool res = false;
            try
            {
                WishList wishList = this.Repository.All<WishList>(
                    ).FirstOrDefault(w => w.PortalId == this.WorkContext.CurrentSite.Id &&
                   w.PortalUserId == this.WorkContext.CurrentUser.Id
                   && w.Id == wishListID
                    && w.Status == WishListStatusTypes.Open);
                if (wishList != null)
                {
                    wishList.Status = WishListStatusTypes.Closed;
                    this.Save(wishList);
                    res = true;
                }
            }
            catch (Exception ex) { throw; }
            return res;
        }

        public bool EditSendMail(string wishListID, bool sendMail)
        {
            bool res = false;
            try
            {
                WishList wishList = this.GetById(wishListID);
                if (wishList != null)
                {
                    wishList.SendMail = sendMail;
                    this.Save(wishList);
                    res = true;
                }
            }
            catch(Exception ex) { throw; }
            return res;
        }
    }
}