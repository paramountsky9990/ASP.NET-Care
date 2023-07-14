using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AspNet.Identity.MongoDB;
using AutoMapper;
using HGP.Common;
using HGP.Common.Database;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Assets;
using HGP.Web.Models.Report;
using HGP.Web.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security.Provider;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StructureMap.Query;
using WebGrease.Css.Extensions;
using HGP.Web.Models.Email;
using System.Web.Configuration;
using System.Web.Mvc;
using HGP.Web.Models.Drafts;

namespace HGP.Web.Services
{
    public interface IAssetService : IBaseService
    {
        void Save(Asset entry);
        Asset GetById(string id);

        void AttachMedia(Asset asset, MediaFileDto file);
        int DeleteAssets(string siteId, IEnumerable<string> assetsToDelete);

        AssetIndexModel BuildAssetIndexModel(ISite site, string hitNumber);
        EditAssetModel BuildEditAssetModel(ISite site, string hitNumber);

        AssetDispositionReportModel BuildDispositionReportModel(string siteId);
        IList<AssetDispositionLineItem> BuildDispositionReportDataModel(string siteId);
        AllAssetsReportDataModel BuildAllAssetsReportModel(string siteId);
        AllAssetsReportDataModel BuildAvailableAssetsReportModel(string siteId);
        IList<AllAssetReportLineItemModel> BuildAvailableAssetsReportDataModel(string siteId);
        IList<AllAssetReportLineItemModel> BuildAllAssetsReportDataModel(string siteId);
        ExpiredAssetsReportDataModel BuildExpiredAssetsReportModel(string siteId);
        IList<ExpiredAssetReportLineItemModel> BuildExpiredAssetsReportDataModel(string siteId);

        void SetAssetStatus(Asset asset, GlobalConstants.AssetStatusTypes assetStatusTypes);
        IList<Category> CalculateCategories(string siteId);

        IList<ManufacturerSummary> CalculateManufacturers(string siteId);

        Asset GetByHitNumber(string siteId, string hitNumber);
        IList<Asset> GetByLocation(string siteId, string hitNumber);

        List<AssetsUploaded> GetAssetUploadSummary(Site site, HttpContext cContext, Int32 intervalInDays);
        List<PendingRequestAssets> GetPendingRequestAssets(Request request, string portalTag, HttpContext cContext);

        List<Asset> GetWishListAssetsMatched(Site site, WishList wishList);
        List<AssetsUploaded> GetWishListDetailedMatchedAssets(Site site, HttpContext cContext, List<Asset> assetsMatched);
        List<Asset> GetExpiringAssets(string siteId, DateTime start, DateTime end);
    }

    public class AssetServiceMappingProfile : Profile
    {
        public AssetServiceMappingProfile()
        {
            CreateMap<Asset, AssetIndexModel>();
            CreateMap<Asset, EditAssetModel>();
            CreateMap<DraftAsset, DraftCreateModel>();
        }
    }


    public class AssetService : BaseService<Asset>, IAssetService
    {
        public AssetService() : this(null, null)
        {
        }

        public AssetService(IMongoRepository repository, IWorkContext workContext)
            : base(repository, workContext)
        {
        }

        public Asset GetByHitNumber(string siteId, string hitNumber)
        {
            return this.Repository.GetAll<Asset>().FirstOrDefault(x => x.PortalId == siteId && x.HitNumber == hitNumber);
        }


        public IList<Asset> GetByLocation(string siteId, string locationName)
        {
            return this.Repository.GetAll<Asset>().Where(x => x.PortalId == siteId && x.Location == locationName).ToList();
        }

        public void AttachMedia(Asset asset, MediaFileDto file)
        {
            if (asset.Media == null)
                asset.Media = new List<MediaFileDto>();
            asset.Media.Add(file);
            asset.Media = asset.Media.OrderBy(x => x.SortOrder).ToList();
            this.Save(asset);
        }

        /// <summary>
        /// Deletes specified id numbers. Not HitNumbers!
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="assetsToDelete"></param>
        /// <returns></returns>
        public int DeleteAssets(string siteId, IEnumerable<string> assetsToDelete)
        {
            var siteService = IoC.Container.GetInstance<SiteService>();
            var site = siteService.GetById(siteId);
            // Get a list of assets that are part of a request, they cannot be deleted

            var allAssetsInRequests =
                (this.Repository.All<Request>()
                    .Where(x => x.PortalId == siteId)
                    .Select(x => x.AssetRequests)).ToList().SelectMany(x => x).Select(y => y.Id).ToList();

            var assetsRemainingToDelete = assetsToDelete.Where(x => !allAssetsInRequests.Contains(x)).ToList();

            assetsRemainingToDelete.ForEach(x => this.DeleteImages(site.SiteSettings.PortalTag, x));
            assetsRemainingToDelete.ForEach(this.Delete);

            siteService.UpdateCategories(siteId);
            siteService.UpdateManufacturers(siteId);

            return assetsRemainingToDelete.Count;
        }

        private void DeleteImages(string portalTag, string assetId)
        {
            var fileNames = new List<string>();
            var asset = this.Repository.GetAll<Asset>().SingleOrDefault(x => x.Id == assetId);
            if ((asset != null) && (asset.Media != null) && (asset.Media.Any()))
            {
                asset.Media.ForEach(x => fileNames.Add(x.FileName));
            }

            if (fileNames.Any())
            {
                var awsService = IoC.Container.GetInstance<AwsService>();
                awsService.DeleteFiles(portalTag, fileNames);
            }
        }

        public AssetIndexModel BuildAssetIndexModel(ISite site, string hitNumber)
        {
            AssetIndexModel model;
            Asset asset;


            model = IoC.Container.GetInstance<ModelFactory>().GetModel<AssetIndexModel>();

            asset = this.GetByHitNumber(site.Id, hitNumber);
            // todo: Throw a 404 if not found
            Mapper.Map<Asset, AssetIndexModel>(asset, model);

            // Find any associated requests for this asset
            model.RequestCount = this.WorkContext.S.RequestService.GetByAssetId(site.Id, asset.Id).Count;

            // Sort the images, this can be removed any time after 7/1/18
            asset.Media = asset.Media.OrderBy(x => x.SortOrder).ToList();

            var userManager = this.WorkContext.HttpContext.GetOwinContext().GetUserManager<PortalUserService>();
            var owner = userManager.FindById(asset.OwnerId);
            model.OwnerName = owner.FirstName + " " + owner.LastName;
            model.OwnerEmail = owner.Email;
            model.OwnerPhone = owner.PhoneNumber;

            var zone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            var utcNow = DateTime.UtcNow;
            var pacificNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone);

            var timeSpan = asset.AvailForSale - pacificNow;
            model.MinutesRemaining = timeSpan.TotalMinutes;

            model.JsonData = JsonConvert.SerializeObject(model);

            if (this.WorkContext.CurrentUser != null)
            {
                // Add to recently viewed
                userManager.AddRecentlyViewedAsset(this.WorkContext.CurrentUser.Id, asset);
            }
            return model;
        }



        public EditAssetModel BuildEditAssetModel(ISite site, string hitNumber)
        {
            EditAssetModel model;
            Asset asset;

            model = IoC.Container.GetInstance<ModelFactory>().GetModel<EditAssetModel>();

            asset = this.GetByHitNumber(site.Id, hitNumber);
            // todo: Throw a 404 if not found
            Mapper.Map<Asset, EditAssetModel>(asset, model);
            model.AsssetId = asset.Id;
            //var userManager = this.WorkContext.HttpContext.GetOwinContext().GetUserManager<PortalUserService>();
            //var owner = userManager.FindById(asset.OwnerId);
            //model.OwnerName = owner.FirstName + " " + owner.LastLogin;
            //model.OwnerEmail = owner.Email;
            //model.OwnerPhone = owner.PhoneNumber;

            return model;
        }

        public AssetDispositionReportModel BuildDispositionReportModel(string siteId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AssetDispositionReportModel>();

            return model;
        }

        public IList<AssetDispositionLineItem> BuildDispositionReportDataModel(string siteId)
        {
            var assets = (from a in this.Repository.All<Asset>()
                          where a.PortalId == siteId
                          orderby a.HitNumber
                          select new
                          {
                              HitNumber = a.HitNumber,
                              ClientIdNumber = a.ClientIdNumber,
                              Status = a.Status,
                              Title = a.Title,
                              IsVisible = a.IsVisible,
                              Manufacturer = a.Manufacturer,
                              ModelNumber = a.ModelNumber,
                              BookValue = a.BookValue,
                              Location = a.Location,
                              Category = a.Category,
                              AvailForRedeploy = a.AvailForRedeploy,
                              AvailForSale = a.AvailForSale,
                              Media = a.Media,
                              // todo: Query too slow, need multikey index
                              //RequestedDate = (from r in this.Repository.All<Request>()
                              //                     where r.AssetRequests.Any(x => x.Id == a.Id) 
                              //                     select r.RequestDate).FirstOrDefault()

                          });

            return assets.ToModelList<AssetDispositionLineItem>();
        }

        public AllAssetsReportDataModel BuildAllAssetsReportModel(string siteId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AllAssetsReportDataModel>();

            return model;
        }
        public IList<AllAssetReportLineItemModel> BuildAllAssetsReportDataModel(string siteId)
        {
            var assets = (from a in this.Repository.All<Asset>()
                          where a.PortalId == siteId
                          orderby a.HitNumber
                          select a).ToModelList<AllAssetReportLineItemModel>();

            return assets.ToModelList<AllAssetReportLineItemModel>();
        }

        public AllAssetsReportDataModel BuildAvailableAssetsReportModel(string siteId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AllAssetsReportDataModel>();

            return model;
        }
        public IList<AllAssetReportLineItemModel> BuildAvailableAssetsReportDataModel(string siteId)
        {
            var assets = (from a in this.Repository.All<Asset>()
                          where a.PortalId == siteId && a.Status == GlobalConstants.AssetStatusTypes.Available && a.AvailForRedeploy < DateTime.UtcNow && a.AvailForSale > DateTime.UtcNow
                          orderby a.HitNumber
                          select a);
            return assets.ToModelList<AllAssetReportLineItemModel>();
        }

        public ExpiredAssetsReportDataModel BuildExpiredAssetsReportModel(string siteId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<ExpiredAssetsReportDataModel>();

            return model;
        }
        public IList<ExpiredAssetReportLineItemModel> BuildExpiredAssetsReportDataModel(string siteId)
        {
            var assets = (from a in this.Repository.All<Asset>()
                          where a.PortalId == siteId && a.AvailForSale < DateTime.UtcNow
                          orderby a.HitNumber
                          select a);
            return assets.ToModelList<ExpiredAssetReportLineItemModel>();
        }
        public void SetAssetStatus(Asset asset, GlobalConstants.AssetStatusTypes assetStatus)
        {
            asset.Status = assetStatus;

            switch (assetStatus)
            {
                case GlobalConstants.AssetStatusTypes.Available:
                    asset.IsVisible = true;
                    break;

                case GlobalConstants.AssetStatusTypes.Requested:
                case GlobalConstants.AssetStatusTypes.Transferred:
                case GlobalConstants.AssetStatusTypes.Unavailable:
                    asset.IsVisible = false;
                    break;
            }
        }

        public IList<Category> CalculateCategories(string siteId)
        {
            var match = new BsonDocument
                {
                                {"$match", new BsonDocument
                                    {
                                        {"PortalId", siteId },
                                        { "AvailForSale", new BsonDocument("$gt", DateTime.Now) },
                                        {"IsVisible", true }
                                    }
                            }
                };

            var group = new BsonDocument
                {
                    { "$group",
                        new BsonDocument
                            {
                                { "_id", new BsonDocument
                                             {
                                                 {
                                                     "Category","$Category"
                                                 }
                                             }
                                },
                                {
                                    "Count", new BsonDocument
                                                 {
                                                     {
                                                         "$sum", 1
                                                     }
                                                 }
                                }
                            }
                  }
                };

            var project = new BsonDocument
                {
                    {
                        "$project",
                        new BsonDocument
                            {
                                {"_id", 0},
                                {"Category","$_id.Category"},
                                {"Count", 1},
                            }
                    }
                };


            var pipeline = new[] { match, group, project };
            var aggregateArgs = new AggregateArgs { Pipeline = pipeline, BatchSize = 0, OutputMode = AggregateOutputMode.Cursor };
            var aggregateResult = this.Repository.GetQuery<Asset>().Aggregate(aggregateArgs);

            var categories = aggregateResult.Select(x =>
                new Category
                {
                    Name = x["Category"].AsString,
                    Count = (int)x["Count"].ToInt64(),
                    UriString = x["Category"].ToString().ToLower().Replace(" ", "-")
                }).OrderBy(x => x.Name).ToList();

            return categories;
        }

        public IList<ManufacturerSummary> CalculateManufacturers(string siteId)
        {
            var match = new BsonDocument
                {
                                {"$match", new BsonDocument
                                    {
                                        {"PortalId", siteId },
                                        { "AvailForSale", new BsonDocument("$gt", DateTime.Now) },
                                        {"IsVisible", true }
                                    }
                            }
                };

            var group = new BsonDocument
                {
                    { "$group",
                        new BsonDocument
                            {
                                { "_id", new BsonDocument
                                             {
                                                 {
                                                     "Manufacturer","$Manufacturer"
                                                 }
                                             }
                                },
                                {
                                    "Count", new BsonDocument
                                                 {
                                                     {
                                                         "$sum", 1
                                                     }
                                                 }
                                }
                            }
                  }
                };

            var project = new BsonDocument
                {
                    {
                        "$project",
                        new BsonDocument
                            {
                                {"_id", 0},
                                {"Manufacturer","$_id.Manufacturer"},
                                {"Count", 1},
                            }
                    }
                };


            var pipeline = new[] { match, group, project };
            var aggregateArgs = new AggregateArgs { Pipeline = pipeline, BatchSize = 0, OutputMode = AggregateOutputMode.Cursor };
            var aggregateResult = this.Repository.GetQuery<Asset>().Aggregate(aggregateArgs);

            var manus = aggregateResult.Select(x =>
                new ManufacturerSummary
                {
                    Name = x["Manufacturer"].ToString(),
                    Count = (int)x["Count"].ToInt64(),
                    UriString = x["Manufacturer"].ToString().ToLower().Replace(" ", "-")
                }).OrderBy(x => x.Name).ToList();

            manus = manus.Where(x => x.Name != "BsonNull").ToList();
            return manus;
        }

        // Get Portal-Wise Uploaded Assets with its Primary-Image & Access-URL 
        public List<AssetsUploaded> GetAssetUploadSummary(Site site, HttpContext cContext, Int32 intervalInDays)
        {
            List<AssetsUploaded> AssetsUploaded = new List<AssetsUploaded>();

            try
            {
                DateTime uploadStart = DateTime.Now.AddDays(intervalInDays);

                IQueryable<Asset> assetList = (from a in this.Repository.All<Asset>()
                                               where a.PortalId == site.Id &&
                                               a.IsVisible == true &&
                                               a.CreatedDate >= uploadStart &&
                                               a.CreatedDate <= DateTime.Now
                                               orderby a.HitNumber
                                               select a).AsQueryable();

                listAssetsWithAccessURLAndPrimaryImageURL(site, cContext, AssetsUploaded, assetList);
            }
            catch (Exception ex) { throw; }
            return AssetsUploaded;
        }

        public List<PendingRequestAssets> GetPendingRequestAssets(Request request, string portalTag, HttpContext cContext)
        {
            List<PendingRequestAssets> PendingRequestAssets = new List<PendingRequestAssets>();
            try
            {
                if (request.AssetRequests != null && request.AssetRequests.Count > 0)
                {
                    foreach (AssetRequestDetail reqAssetDetail in request.AssetRequests)
                    {
                        if (reqAssetDetail.Status == GlobalConstants.RequestStatusTypes.Open || reqAssetDetail.Status == GlobalConstants.RequestStatusTypes.Pending)
                        {
                            PendingRequestAssets PendingRequestAsset = new PendingRequestAssets();
                            PendingRequestAsset.PrimaryImageURL = string.Empty;

                            Asset asset = this.GetById(reqAssetDetail.Id);
                            if (asset != null && asset.Media != null & asset.Media.Count > 0)
                            {
                                PendingRequestAsset.PrimaryImageURL = "https://s3-us-west-1.amazonaws.com/hgpmedia/" + portalTag + "/l/" + asset.Media[0].FileName;
                            }

                            PendingRequestAsset.Asset = asset;

                            PendingRequestAssets.Add(PendingRequestAsset);
                        }
                    }
                }
            }
            catch (Exception ex) { throw; }
            return PendingRequestAssets;
        }
        
        public List<Asset> GetWishListAssetsMatched(Site site, WishList wishList)
        {
            List<Asset> wishlistMatchedAssets = new List<Asset>();
            try
            {
                IQueryable<Asset> matchedAssets = this.Repository.TextSearch<Asset>(wishList.SearchCriteria, site.Id)
                                               .Where(a => a.PortalId == site.Id
                                               && a.IsVisible == true
                                               ).AsQueryable();
                if (matchedAssets != null && matchedAssets.Count() > 0)
                {
                    wishlistMatchedAssets = matchedAssets.ToList();
                }
            }
            catch(Exception ex) { throw; }
            return wishlistMatchedAssets;
        }

        public List<AssetsUploaded> GetWishListDetailedMatchedAssets(Site site, HttpContext cContext, List<Asset> assetsMatched)
        {
            List<AssetsUploaded> AssetsUploaded = new List<AssetsUploaded>();
            try
            {
                if (assetsMatched != null && assetsMatched.Count > 0)
                {

                    listAssetsWithAccessURLAndPrimaryImageURL(site, cContext, AssetsUploaded, assetsMatched.AsQueryable());
                }
            }
            catch (Exception ex) { throw; }
            return AssetsUploaded;
        }



        private static void listAssetsWithAccessURLAndPrimaryImageURL(Site site, HttpContext cContext, List<AssetsUploaded> AssetsUploaded, IQueryable<Asset> assetList)
        {
            if (assetList != null && assetList.ToList().Count > 0)
            {
                foreach (Asset asset in assetList)
                {
                    string assetURL = string.Empty;
                    string primaryImageURL = string.Empty;
                    string matchedAssetsURL = string.Empty;

                    string baseUrl = cContext.Request.Url.Authority; 

                    if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("::1"))
                    {
                        baseUrl = WebConfigurationManager.AppSettings["JobSchedulerLocalhost"];
                    }

                    string controllerUrl = "/" + site.SiteSettings.PortalTag + "/asset/index/" + asset.HitNumber;
                    assetURL = "https://" + baseUrl + controllerUrl;
                    
                    if (asset != null && asset.Media != null && asset.Media.Count > 0)
                    {
                        primaryImageURL = "https://s3-us-west-1.amazonaws.com/hgpmedia/" + site.SiteSettings.PortalTag + "/l/" + asset.Media[0].FileName;
                    }
                    
                    AssetsUploaded assetUploaded = new AssetsUploaded();
                    assetUploaded.Asset = asset;
                    assetUploaded.AssetURL = assetURL;
                    assetUploaded.PrimaryImageURL = primaryImageURL;

                    AssetsUploaded.Add(assetUploaded);

                }
            }
        }

        public List<Asset> GetExpiringAssets(string siteId, DateTime start, DateTime end)
        {
            start = start.ToUniversalTime();
            end = end.ToUniversalTime();
            var assets = (from a in this.Repository.All<Asset>()
                          where a.PortalId == siteId && a.Status == GlobalConstants.AssetStatusTypes.Available && a.AvailForSale <= end && a.AvailForSale >= start
                          orderby a.HitNumber
                          select a).ToList();

            return assets;
        }
    }

    public static class MongoExtensions
    {
        // From: http://mikaelkoskinen.net/mongodb-aggregation-framework-examples-in-c/
        public static dynamic ToDynamic(this BsonDocument doc)
        {
            var json = doc.ToJson();
            dynamic obj = JToken.Parse(json);
            return obj;
        }
    }

}