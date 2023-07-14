#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using AutoMapper;
using HGP.Common;
using HGP.Common.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Assets;
using HGP.Web.Models.List;
using HGP.Web.Models.Requests;
using HGP.Web.Models.Settings;
using HGP.Web.Utilities;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using WebGrease.Css.Extensions;
using Site = HGP.Web.Models.Site;

#endregion

namespace HGP.Web.Services
{
    public interface ISiteService : IBaseService
    {
        void Save(Site site);
        void Delete(Site site);
        void Delete(string siteId);
        void Exists(Site site);
        Site GetById(string id);
        ISite GetByPortalTag(string hostName);
        void SetLastLogin(string siteId);
        AdminHomeModel BuildAdminHomeModel();
        SiteCreateModel BuildSiteCreateModel();
        AdminAssetsHomeModel BuildAdminAssetsHomeModel(ISite site, int rows = 25, int page = 1);
        PortalHomeModel BuildPortalHomeModel(ISite site, PortalUser user);
        void CreateBucket(ISite site);
        AdminLocationsHomeModel BuildAdminLocationsHomeModel(string siteId);
        SiteSettingsHomeModel BuildSiteSettingsHomeModel(string siteId, string registrationUrl);
        IList<Location> GetLocations(string siteId);
        void UpdateCategories(string siteId);
        void UpdateManufacturers(string siteId);
        AdminSiteSettingsModel BuildAdminSiteSettingsModel(string siteId);
        List<string> GetHitNumbers(string siteId);
        Location GetLocationByName(string siteId, string locationName);
        void UpdateAllAssetCounts();
        List<ApprovalStep> GetDefaultProcess();
        List<PortalUser> GetOwners(string siteId);
        List<PortalUser> GetAdmins(string siteId);
    }

    public class SiteServiceMappingProfile : Profile
    {
        public SiteServiceMappingProfile()
        {
            CreateMap<MediaFileDto, AdminAssetsHomeGridMediaModel>();
            CreateMap<Asset, AdminAssetsHomeGridModel>();
            CreateMap<Asset, ListHomeGridModel>();
            CreateMap<SiteSettings, SiteSettingsHomeModel>();
            CreateMap<SiteSettings, AdminSiteSettingsModel>();
            CreateMap<Site, AdminSiteSettingsModel>();
            CreateMap<MediaFile, MediaFileDto>();
            CreateMap<ApprovalStep, ApprovalStepDto>();

        }
    }

    public class SiteService : BaseService<Site>, ISiteService
    {
        private readonly IAssetService AssetService;
        private readonly IAwsService AwsService;

        public SiteService()
            : this(null, null, null, null)
        {
        }

        public SiteService(IMongoRepository repository, IWorkContext workContext, IAssetService assetService,
            IAwsService awsService)
            : base(repository, workContext)
        {
            AwsService = awsService ?? IoC.Container.GetInstance<IAwsService>();
            AssetService = assetService ?? IoC.Container.GetInstance<IAssetService>();
        }

        #region Implementation of ISiteService

        public void Delete(Site site)
        {
            Delete(site.Id);
        }

        public new void Delete(string siteId)
        {
            // Add business logic to remove a site here
            // todo: Delete associated objects (assets etc.)

            var site = GetById(siteId);
            AssetService.Repository.Delete<Asset>(x => x.PortalId == site.Id);
            var userService = IoC.Container.GetInstance<PortalUserService>();
            userService.DeleteAllFromSite(site.Id);
            IoC.Container.GetInstance<IRequestService>().Repository.Delete<Request>(x => x.PortalId == site.Id);

            AwsService.TryRemoveBucket(site.SiteSettings.PortalTag);

            base.Delete(siteId);
        }

        public void Exists(Site entry)
        {
            throw new NotImplementedException();
        }

        public ISite GetByPortalTag(string portalTag)
        {
            if (string.IsNullOrEmpty(portalTag))
                return null;

            var site =
                Repository.All<Site>()
                    .SingleOrDefault(x => x.SiteSettings.PortalTag.ToLower() == portalTag.ToLowerInvariant());
                // todo: Change to All(), move to repo

            if (site != null)
            {
                site.IsNew = false;
            }

            return site;
        }

        public void SetLastLogin(string siteId)
        {
            var site = this.GetById(siteId);
            site.SiteSettings.LastLogin = DateTime.UtcNow;
            this.Save((Site)site);
        }

        public AdminHomeModel BuildAdminHomeModel()
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminHomeModel>();

            var allSites = (from s in Repository.All<Site>()
                where s.SiteSettings.IsAdminPortal == false
                select new AdminHomeSiteGridModel
                {
                    Id = s.Id,
                    PortalTag = s.SiteSettings.PortalTag,
                    IsOpen = s.SiteSettings.IsOpen,
                    CompanyName = s.SiteSettings.CompanyName,
                    AccountExecutive = s.AccountExecutive,
                    LastLogin = s.SiteSettings.LastLogin,
                    AssetCount = Repository.All<Asset>().Count(x => x.PortalId == s.Id),
                    PendingTransfersCount =
                        Repository.All<Request>()
                            .Count(x => x.PortalId == s.Id && x.Status == GlobalConstants.RequestStatusTypes.Pending),
                    UsersCount = Repository.All<PortalUser>().Count(x => x.PortalId == s.Id),
                }
                );

            model.Sites = allSites.ToList();
            model.CurrentDatabase = WebConfigurationManager.AppSettings["MongoDbName"];
            model.JsonData = JsonConvert.SerializeObject(allSites);

            return model;
        }

        public SiteCreateModel BuildSiteCreateModel()
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<SiteCreateModel>();

            var adminSite = this.GetByPortalTag("admin");
            var allAEs = (from s in Repository.All<PortalUser>()
                            where s.PortalId == adminSite.Id 
                            orderby s.FirstName
                              select s).ToModelList<ContactInfo>();

            model.AEs = allAEs;
            model.CurrentAe = "";
            model.JsonData = JsonConvert.SerializeObject(model);

            return model;
        }
        public AdminAssetsHomeModel BuildAdminAssetsHomeModel(ISite site, int rows = 25, int page = 1)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminAssetsHomeModel>();

            var assets = (from s in Repository.All<Asset>()
                          where s.PortalId == site.Id
                          orderby s.UpdatedDate descending
                          select new
                          {
                              Id = s.Id,
                              HitNumber = s.HitNumber,
                              ClientIdNumber = s.ClientIdNumber,
                              OwnerId = s.OwnerId,
                              Title = s.Title,
                              Status = s.Status,
                              IsVisible = s.IsVisible,
                              Manufacturer = s.Manufacturer,
                              ModelNumber = s.ModelNumber,
                              SerialNumber = s.SerialNumber,
                              BookValue = s.BookValue,
                              Location = s.Location,
                              ServiceStatus = s.ServiceStatus,
                              Condition = s.Condition,
                              Category = s.Category,
                              Catalog = s.Catalog,
                              Media = s.Media,
                              Images = s.Media.Count,
                              AvailForRedeploy = s.AvailForRedeploy,
                              AvailForSale = s.AvailForSale
                          }).Take(rows).Skip(page - 1).ToList();

            var mappedAssets = assets.ToModelList<AdminAssetsHomeGridModel>();

            // Load all users who might be an owener of an asset
            var adminSite = IoC.Container.GetInstance<ISiteService>().GetByPortalTag("admin");
            var users = Repository.All<PortalUser>()
                    .Where(x => x.PortalId == site.Id || x.PortalId == adminSite.Id)
                    .Select(x => new { x.Id, x.FirstName, x.LastName }).ToList();
            // Grab their name out of the preloaded user list
            foreach (var asset in mappedAssets)
            {
                var userData = users.FirstOrDefault(x => x.Id == asset.OwnerId);
                if (userData != null)
                    asset.OwnerName = userData.FirstName + ' ' + userData.LastName;
            }
            model.Assets = mappedAssets;

            return model;
        }

        public PortalHomeModel BuildPortalHomeModel(ISite site, PortalUser user)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<PortalHomeModel>();

            var assets = (from s in Repository.All<Asset>()
                where s.PortalId == site.Id && s.Media.Count > 0 && s.IsVisible == true
                orderby s.UpdatedDate descending
                select s).Take(18);

            var mappedAssets = Mapper.Map<IList<Asset>, IList<ListHomeGridModel>>(assets.ToList());

            model.RecentlyAddedAssets = mappedAssets;
            model.RecentlyViewedAssets = WorkContext.CurrentUser.RecentlyViewed.ToModelList<ListHomeGridModel>();
            model.RecentCategories = user.RecentCategories.Where(x => x.PortalId == site.Id);
            model.RecentSearches = user.RecentSearches;
            if (site.Categories != null)
                model.AllCategories = site.Categories.Where(x => x.Count > 0).OrderBy(x => x.Name).ToModelList<RecentCategory>();
            else
                model.AllCategories = new List<RecentCategory>();
            model.AllManufacturers = site.Manufacturers.Where(x => x.Count > 0).OrderBy(x => x.Name).ToModelList<RecentCategory>();
            model.JsonData = JsonConvert.SerializeObject(model);

            return model;
        }

        public void CreateBucket(ISite site)
        {
            Contract.Assert(site != null);
            AwsService.TryCreateBucket(site.SiteSettings.PortalTag);
        }

        public AdminLocationsHomeModel BuildAdminLocationsHomeModel(string siteId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminLocationsHomeModel>();

            var site = (from s in Repository.All<Site>()
                where s.Id == siteId
                select s).FirstOrDefault();

            if (site == null)
                return null;

            model.SiteId = site.Id;
            model.PortalTag = site.SiteSettings.PortalTag;
            model.Locations = (from l in site.Locations
                               select new AdminLocationsHomeGridModel
                                {
                                    Name = l.Name,
                                    Address = l.Address,
                                    OwnerId = l.OwnerId,
                                    OwnerName = l.OwnerName,
                                    OwnerPhone = l.OwnerPhone,
                                    OwnerEmail = l.OwnerEmail,
                                    VisibleAssetCount = Repository.All<Asset>().Count(x => x.PortalId == model.SiteId && x.Location.ToLowerInvariant() == l.Name.ToLowerInvariant() && x.IsVisible == true),
                                    HiddenAssetCount = Repository.All<Asset>().Count(x => x.PortalId == model.SiteId && x.Location.ToLowerInvariant() == l.Name.ToLowerInvariant() && x.IsVisible == false)
                                }).ToList();

            model.JsonData = JsonConvert.SerializeObject(model);

            return model;
        }

        public SiteSettingsHomeModel BuildSiteSettingsHomeModel(string siteId, string registrationUrl)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<SiteSettingsHomeModel>();

            var site = (from s in Repository.All<Site>()
                where s.Id == siteId
                select s).FirstOrDefault();

            if (site == null)
                return null;

            Mapper.Map(site.SiteSettings, model);

            model.Id = site.Id;
            model.SiteId = site.Id;

            if (!string.IsNullOrWhiteSpace(model.HomePageMessage))
                model.HomePageMessageNoHtml = Regex.Replace(model.HomePageMessage, @"<[^>]*>", String.Empty);

            if (!string.IsNullOrWhiteSpace(model.RegistrationMessage))
                model.RegistrationMessageNoHtml = Regex.Replace(model.RegistrationMessage, @"<[^>]*>", String.Empty);

            if (!string.IsNullOrWhiteSpace(model.CustomCss))
                model.CustomCssPreview = Regex.Replace(model.CustomCss.Left(100), @"<[^>]*>", String.Empty);

            if (!string.IsNullOrWhiteSpace(model.RequestPageMessage))
                model.RequestPageMessagesPreview = Regex.Replace(model.RequestPageMessage, @"<[^>]*>", String.Empty);

            model.RegistrationUrl = registrationUrl;

            return model;
        }

        public IList<Location> GetLocations(string siteId)
        {
            var site = (from s in Repository.All<Site>()
                where s.Id == siteId
                select s).FirstOrDefault();

            return site == null ? null : site.Locations;
        }

        public List<PortalUser> GetOwners(string siteId)
        {
            // Get the id af the admin portal
            var adminSiteId = GetByPortalTag("admin").Id;

            // Return all users from the current site plus the admin site
            var users = (from s in Repository.All<PortalUser>()
                        where s.PortalId == siteId || s.PortalId == adminSiteId
                         select s).ToList();

            return users;
        }

        public void UpdateCategories(string siteId)
        {
            var categories = AssetService.CalculateCategories(siteId);
            var site = GetById(siteId);

            site.Categories = categories;

            Save(site);

            var wc = IoC.Container.GetInstance<IWorkContext>();

            if ((wc.CurrentSite != null) && (wc.CurrentSite.Id == siteId))
                wc.CurrentSite.Categories = categories;
        }

        public void UpdateManufacturers(string siteId)
        {
            var manus = AssetService.CalculateManufacturers(siteId);
            var site = GetById(siteId);

            site.Manufacturers = manus;

            Save(site);

            var wc = IoC.Container.GetInstance<IWorkContext>();

            if ((wc.CurrentSite != null) && (wc.CurrentSite.Id == siteId))
                wc.CurrentSite.Manufacturers = manus;
        }

        public AdminSiteSettingsModel BuildAdminSiteSettingsModel(string siteId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminSiteSettingsModel>();

            var site = (from s in Repository.All<Site>()
                where s.Id == siteId
                select s).FirstOrDefault();

            if (site == null)
                return null;

            Mapper.Map(site, model);
            Mapper.Map(site.SiteSettings, model);

            model.SiteId = site.Id;

            if (!string.IsNullOrWhiteSpace(model.HomePageMessage))
                model.HomePageMessageNoHtml = Regex.Replace(model.HomePageMessage, @"<[^>]*>", String.Empty);

            if (!string.IsNullOrWhiteSpace(model.RegistrationMessage))
                model.RegistrationMessageNoHtml = Regex.Replace(model.RegistrationMessage, @"<[^>]*>", String.Empty);

            if (!string.IsNullOrWhiteSpace(model.CustomCss))
                model.CustomCssPreview = Regex.Replace(model.CustomCss.Left(100), @"<[^>]*>", String.Empty);

            return model;
        }

        public List<string> GetHitNumbers(string siteId)
        {
            var hitNumbers = (from s in Repository.All<Asset>()
                          where s.PortalId == siteId
                          orderby s.HitNumber descending
                          select s.HitNumber).ToList();

            return hitNumbers;
        }

        public Location GetLocationByName(string portalTag, string locationName)
        {
            var site = (from s in Repository.All<Site>()
                        where s.SiteSettings.PortalTag == portalTag
                        select s).FirstOrDefault();

            return site.Locations.FirstOrDefault(x => x.Name == locationName);
        }

        public void UpdateAllAssetCounts()
        {
            var sites = (from s in Repository.All<Site>()
                        select s.Id).ToList();

            if (sites.Any())
            {
                sites.ForEach(this.UpdateCategories);
                sites.ForEach(this.UpdateManufacturers);
            }
        }

        #endregion

        public List<ApprovalStep> GetDefaultProcess()
        {
            var steps = new List<ApprovalStep>();
            steps.Add(new ApprovalStep() { Action = "initiate-request", TaskType = "action" });
            steps.Add(new ApprovalStep() { Action = "notify-manager", TaskType = "action" });
            steps.Add(new ApprovalStep() { Action = "wait-manager", TaskType = "decision" });
            steps.Add(new ApprovalStep() { Action = "notify-requestor", TaskType = "action" });
            steps.Add(new ApprovalStep() { Action = "notify-others", TaskType = "action" });
            steps.Add(new ApprovalStep() { Action = "complete-request", TaskType = "action" });

            return steps;
        }

        public List<PortalUser> GetAdmins(string siteId)
        {
            // Return all users from the current site plus the admin site
            var users = (from s in Repository.All<PortalUser>()
                where (s.PortalId == siteId && s.Roles.Contains("ClientAdmin"))
                select s).ToList();

            return users;
        }
}
}