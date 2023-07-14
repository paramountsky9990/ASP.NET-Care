using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AspNet.Identity.MongoDB;
using AutoMapper;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Assets;
using HGP.Web.Models.Email;
using HGP.Web.Models.List;
using HGP.Web.Utilities;
using Microsoft.AspNet.Identity.Owin;
using Newtonsoft.Json;


using HGP.Web.Models.Report;

namespace HGP.Web.Services
{
    public class ListHomeModelResults
    {
        public List<ListHomeGridModel> PagedAssets { get; set; }
        public int ResultCount { get; set; }
        public IEnumerable<KeyValuePair<string, int>> LocationCounts { get; set; }
        public string LocationUri { get; set; }
        public string CategoryUri { get; set; }
        public string SearchText { get; set; }
}
    public interface IListService
    {
        ListHomeModelResults BuildListAssetsPage(Site site, string categoryUri, string locationUri, string searchText = "", int page = 1, int itemsPerPage = 10);
        ListHomeModel BuildListHomeModel(Site site, PortalUser user, string categoryUri, string locationUri, string searchText = "", int page = 1, int itemsPerPage = 10 );

        IList<AllAssetReportLineItemModel> ListAllAssetsDataModel(Site site, string categoryUri, string locationUri, string search);


    }

    public class ListServiceMappingProfile : Profile
    {
        public ListServiceMappingProfile()
        {
            CreateMap<MediaFileDto, AdminAssetsHomeGridMediaModel>();
            CreateMap<Asset, ListHomeGridModel>();
        }
    }

    public class ListService : IListService 
    {
        public IAssetService AssetService { get; set; }

        public ListService(IAssetService assetService, IRequestService requestService/*, IPortalUserService userService*/)
        {
            this.AssetService = assetService;
            this.RequestService = requestService;
            var userStore = new UserStore<PortalUser>(ApplicationIdentityContext.Create());
            this.UserService = new PortalUserService(userStore);

            //this.userService = IoC.Container.GetInstance<IPortalUserService>(); // todo: Change back to IoC
        }

        public ListHomeModelResults BuildListAssetsPage(Site site, string categoryUri, string locationUri, string searchText = "", int page = 1, int itemsPerPage = 10)
        {
            // Convert the category uri to a category string
            var categoryName = "";
            if (site.Categories != null)
            {
                var category = site.Categories.FirstOrDefault(x => x.UriString == categoryUri.ToLowerInvariant());
                if (category != null)
                    categoryName = category.Name;
            }

            var locationName = "";
            if (site.Locations != null)
            {
                var location = site.Locations.FirstOrDefault(x => x.Name.ToLowerInvariant() == HttpUtility.UrlDecode(locationUri.ToLowerInvariant()));
                if (location != null)
                    locationName = location.Name;
            }

            List<string> requestedAssetIds = new List<string>();
            if (!site.SiteSettings.Features.Contains("allowmultiplerequests"))
            {
                // Build a list of all pending assets so they can be hidden
                requestedAssetIds = this.RequestService.GetRequestedAssetIds(site.Id);
            }

            var utcNow = DateTime.UtcNow;

            IQueryable<Asset> assetsQuery;
            // Create a queryable for all visible assets minus any that are part of a request
            if (string.IsNullOrWhiteSpace(searchText))
            {
                assetsQuery = (from a in this.AssetService.Repository.All<Asset>()
                              where a.PortalId == site.Id  && a.IsVisible == true && (a.AvailForSale > utcNow) && !requestedAssetIds.Contains(a.Id)
                              select a);
            }
            else
            {
                assetsQuery = this.AssetService.Repository.TextSearch<Asset>(searchText, site.Id).AsQueryable();
                // Filter down to available assets
                assetsQuery = (from a in assetsQuery
                               where a.PortalId == site.Id && a.IsVisible == true && (a.AvailForSale > utcNow) && !requestedAssetIds.Contains(a.Id)
                               select a);
            }

            // Create a queryable for all visible assets minus any that are part of a request
            assetsQuery = from a in assetsQuery
                where a.PortalId == site.Id && a.IsVisible == true && !requestedAssetIds.Contains(a.Id)
                select a;

            // Restrict to a category if one is supplied
            if (!string.IsNullOrEmpty(categoryName))
                assetsQuery = assetsQuery.Where(x => x.Category == categoryName);

            // Determine location counts
            var allLocations = assetsQuery.Select(x => x.Location).ToList();
            var groupedLocations = (from x in allLocations
                                    group x by x into grp
                                    select new KeyValuePair<string, int>(grp.Key, grp.Count())).ToList();
            var allCount = groupedLocations.Sum(x => x.Value);
            groupedLocations.Add(new KeyValuePair<string, int>("All", allCount));
            
            // Restrict to a location if one is supplied
            if (!string.IsNullOrEmpty(locationName))
                assetsQuery = assetsQuery.Where(x => x.Location == locationName);

            //Convert result set to a list
            var assetList = assetsQuery.ToList();
            var resultCount = assetsQuery.Count();

            // Narrow down to a particular page
            var pagedAssetList = assetList.Skip((page - 1) * itemsPerPage).Take(itemsPerPage).OrderBy(x => x.HitNumber).ToList();


            try
            {
                // Grab the data and map
                var mappedAssets = Mapper.Map<List<Asset>, List<ListHomeGridModel>>(pagedAssetList);

                var index = (page - 1) * itemsPerPage;
                foreach (var asset in mappedAssets)
                {
                    asset.Sequence = ++index;

                    var timeSpan = asset.AvailForSale - utcNow;
                    asset.MinutesRemaining = timeSpan.TotalMinutes;
                }

                var result = new ListHomeModelResults()
                {
                    PagedAssets = mappedAssets,
                    ResultCount = resultCount,
                    LocationCounts = groupedLocations,
                    LocationUri = locationUri,
                    CategoryUri = categoryUri,
                    SearchText = searchText
                };
                return result;
            }
            catch (Exception)
            {
                
                throw;
            }

        }

        public ListHomeModel BuildListHomeModel(Site site, PortalUser user, string categoryUri, string locationUri, string searchText = "", int page = 1, int itemsPerPage = 10)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<ListHomeModel>();

            var mappedAssets = BuildListAssetsPage(site, categoryUri, locationUri, searchText, page, itemsPerPage);

            // Configure categories dropdown
            var actualCategories = new List<CategoryListModel>();
            if (site.Categories != null)
                actualCategories = site.Categories.ToModelList<CategoryListModel>() as List<CategoryListModel>;

            var rootCategory = new CategoryListModel()
            {
                Name = "All",
                Count = actualCategories.Sum(x => x.Count),
                UriString = "",
                IsActive = false
            };

            actualCategories.Insert(0, rootCategory);
            foreach (var categoryListModel in actualCategories.Where(categoryListModel => categoryListModel.UriString == categoryUri))
            {
                categoryListModel.IsActive = true;
            }

            foreach (var categoryListModel in actualCategories)
            {
                categoryListModel.ActionName = "index";
                categoryListModel.ControllerName = "list";
                categoryListModel.LinkText = string.Format("{0} ({1})", categoryListModel.Name.Left(26), categoryListModel.Count);
                categoryListModel.Tag = categoryListModel.UriString;
            }

            // Configure locations dropdown
            var actualLocations = new List<LocationListModel>();
            if (site.Locations != null)
                actualLocations = site.Locations.ToModelList<LocationListModel>() as List<LocationListModel>;

            var rootLocation= new LocationListModel()
            {
                Name = "All",
                Count = actualLocations.Sum(x => x.Count),
                UriString = "",
                IsActive = false,
                IsVisible = true
            };

            actualLocations.Insert(0, rootLocation);
            foreach (var locationListModel in actualLocations)
            {
                locationListModel.ActionName = "index";
                locationListModel.ControllerName = "list";
                locationListModel.LinkText = string.Format("{0} ({1})", locationListModel.Name, GetLocationCount(locationListModel.Name, mappedAssets.LocationCounts));
                locationListModel.UriString = HttpUtility.UrlEncode(locationListModel.Name);
                locationListModel.Tag = locationListModel.UriString;
                locationListModel.IsVisible = false;
                locationListModel.Count = GetLocationCount(locationListModel.Name, mappedAssets.LocationCounts);
            }

            foreach (var locationListModel in actualLocations.Where(x => x.UriString == locationUri))
                locationListModel.IsActive = true;

            foreach (var locationListModel in actualLocations.Where(x => x.Count > 0))
                locationListModel.IsVisible = true;

            // Log the browse request
            if (!string.IsNullOrEmpty(categoryUri))
            {
                IoC.Container.GetInstance<IActivityLogService>().LogCategoryBrowse(site.SiteSettings.PortalTag, user.UserName, categoryUri, mappedAssets.ResultCount);
                this.UserService.AddRecentCategory(site, user.Id, categoryUri);
            }

            // Log the search request
            if (!string.IsNullOrEmpty(searchText))
            {
                IoC.Container.GetInstance<IActivityLogService>().LogSearch(site.SiteSettings.PortalTag, user.UserName, searchText, mappedAssets.ResultCount);
                this.UserService.AddRecentSearch(site, user.Id, searchText);
            }

            model.Assets = mappedAssets.PagedAssets;
            model.HeaderModel.ResultCount = mappedAssets.ResultCount;
            model.HeaderModel.SearchText = searchText;
            model.SearchText = searchText;
            model.ResultCount = mappedAssets.ResultCount;
            model.Categories = actualCategories;
            model.CategoryUri = categoryUri;
            model.LocationUri = locationUri;
            model.Locations = actualLocations;
            model.PageNumber = 1; // The first page is generated by this method, remaining.po pages use ajax
            model.JsonData = JsonConvert.SerializeObject(model);
         
            return model;
        }

        private int GetLocationCount(string name, IEnumerable<KeyValuePair<string, int>> locationCounts)
        {
            return locationCounts.FirstOrDefault(x => x.Key.ToLowerInvariant() == name.ToLowerInvariant()).Value;
        }

        private bool GetIsActive(string uri, string currentUri)
        {
            return uri == currentUri;
        }



        public IList<AllAssetReportLineItemModel> ListAllAssetsDataModel(Site site, string categoryUri, string locationUri, string search)
        {
            IQueryable<Asset> assetsQuery;
            var utcNow = DateTime.UtcNow;
            var requestedAssetIds = this.RequestService.GetRequestedAssetIds(site.Id);

            var categoryName = "";
            if (site.Categories != null)
            {
                var category = site.Categories.FirstOrDefault(x => x.UriString == categoryUri.ToLowerInvariant());
                if (category != null)
                    categoryName = category.Name;
            }

            var locationName = "";
            if (site.Locations != null)
            {
                var location = site.Locations.FirstOrDefault(x => x.Name.ToLowerInvariant() == HttpUtility.UrlDecode(locationUri.ToLowerInvariant()));
                if (location != null)
                    locationName = location.Name;
            }

            if (!(string.IsNullOrEmpty(search)))
            {
                assetsQuery = this.AssetService.Repository.TextSearch<Asset>(search, site.Id).AsQueryable();
              
                assetsQuery = (from a in assetsQuery
                               where a.PortalId == site.Id && a.IsVisible == true && (a.AvailForSale > utcNow) && !requestedAssetIds.Contains(a.Id)
                               select a);
            }
            else
            {
                assetsQuery = (from a in this.AssetService.Repository.All<Asset>()
                               where a.PortalId == site.Id && a.IsVisible == true && (a.AvailForSale > utcNow) && !requestedAssetIds.Contains(a.Id)
                               select a);
            }


           
            assetsQuery = from a in assetsQuery
                          where a.PortalId == site.Id && a.IsVisible == true && !requestedAssetIds.Contains(a.Id)
                          select a;

          
            if (!string.IsNullOrEmpty(categoryName))
                assetsQuery = assetsQuery.Where(x => x.Category==categoryName);

            if (!string.IsNullOrEmpty(locationName))
                assetsQuery = assetsQuery.Where(x => x.Location == locationName);

            

            var siteService = IoC.Container.GetInstance<ISiteService>();

            var portalTag = siteService.GetById(site.Id).SiteSettings.PortalTag;
            foreach (var asset in assetsQuery)
            {
                asset.PortalId = portalTag;
            }
            
            return assetsQuery.ToModelList<AllAssetReportLineItemModel>();
        }






        public IRequestService RequestService { get; set; }

        public PortalUserService UserService { get; set; }
    }


}