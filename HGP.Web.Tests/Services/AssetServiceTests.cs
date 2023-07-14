using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using AspNet.Identity.MongoDB;
using AutoMapper.Internal;
using HGP.Common;
using HGP.Common.Database;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;

namespace HGP.Web.Tests.Services
{
    [TestFixture]
    public class AssetServiceTests
    {
        #region Additional test attributes

        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        [OneTimeSetUp]
        public static void MyClassInitialize()
        {
            RegisterDependencies.InitStructureMap();
            RegisterDependencies.InitMongo();

            S3BucketUtil.CreateRootBucket();
        }

        //Use ClassCleanup to run code after all tests in a class have run
        [OneTimeTearDown]
        public static void MyClassCleanup()
        {
            var repository = IoC.Container.GetInstance<IMongoRepository>();
            repository.AllowDatabaseDrop = true;
            repository.DropDatabase();

            S3BucketUtil.RemoveRootBucket();
        }

        //Use TestInitialize to run code before running each test
        [SetUp]
        public void MyTestInitialize()
        {
            var repository = IoC.Container.GetInstance<IMongoRepository>();
            repository.AllowDatabaseDrop = true;
            repository.DropDatabase();

            var client = new MongoClient(WebConfigurationManager.AppSettings["MongoDbConnectionString"]);
            //todo: Move to config file
            var database = client.GetServer().GetDatabase("CareDbTests");
            var users = database.GetCollection<IdentityUser>("PortalUsers");
            var roles = database.GetCollection<IdentityRole>("Roles");
            this.UserStore = new UserStore<PortalUser>(new ApplicationIdentityContext(users, roles));
            var userManager = new PortalUserService(this.UserStore);

            IoC.Container.Configure(x => x.For<PortalUserService>().Use(userManager));


        }

        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //

        public UserStore<PortalUser> UserStore { get; set; }

        #endregion

        [Test]
        public void RequestServiceConstructorTest()
        {
            var service = new RequestService();
            Assert.IsNotNull(service);
        }

        [Test]
        public async void SaveOneAssetTest()
        {var service = new AssetService();
            var userManager = new PortalUserService(this.UserStore);

            var siteService = new SiteService();
            var site = new Site();


            var asset = new Asset() { Id = "AssetId", PortalId = "ASiteId", OwnerId = "AnOwnerId", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            service.Save(asset);

            var target = service.GetById(asset.Id);
            Assert.NotNull(target);
            Assert.That(target.Id, Is.EqualTo("AssetId"));
        }

        private async Task<Site> AddSiteRequest()
        {
            var service = new RequestService();
            var userManager = new PortalUserService(this.UserStore);

            var siteService = new SiteService();
            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            try
            {
                var result = await userManager.CreateAsync(requestor, "123456");
                Assert.True(result.Succeeded);

            }
            catch (Exception)
            {
                
                throw;
            }
            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            var assetService = new AssetService();
            assetService.Save(asset);
            service.AddToRequest(asset, requestor);

            return site;
        }

        [Test]
        public async void DeleteOneAssetNoRequestTest()
        {
            var service = new AssetService();

            var asset = new Asset() { Id = "AssetId1", PortalId = "SiteId", OwnerId = "AnOwnerId", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title1" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = "SiteId", OwnerId = "AnOwnerId", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);

            var idList = new List<string>();
            idList.Add("AssetId1");
            var result = service.DeleteAssets("SiteId", idList);
            Assert.That(result, Is.EqualTo(1));

            var target = service.GetById("AssetId1");
            Assert.Null(target);

            var target2 = service.GetById("AssetId2");
            Assert.NotNull(target2);
        }


        [Test]
        public async void DeleteOneAssetWithRequestTest()
        {
            var service = new AssetService();

            var site = await AddSiteRequest();

            var asset = new Asset() { Id = "AssetId1", PortalId = site.Id, OwnerId = "AnOwnerId", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title1" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = "AnOwnerId", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);

            var idList = new List<string>();
            idList.Add("AssetId1");
            var result = service.DeleteAssets(site.Id, idList);
            Assert.That(result, Is.EqualTo(1));

            var target = service.GetById("AssetId1");
            Assert.Null(target);

            var target2 = service.GetById("AssetId2");
            Assert.NotNull(target2);
        }

        [Test]
        public async void DeleteTwoAssetsWithRequestTest()
        {
            var service = new AssetService();

            var site = await AddSiteRequest();

            var asset = new Asset() { Id = "AssetId1", PortalId = site.Id, OwnerId = "AnOwnerId", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title1" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = "AnOwnerId", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);

            var idList = new List<string>();
            idList.Add("AssetId2");
            idList.Add("AssetId1");
            var result = service.DeleteAssets(site.Id, idList);
            Assert.That(result, Is.EqualTo(2));

            var target = service.GetById("AssetId");
            Assert.NotNull(target);

            var target1 = service.GetById("AssetId1");
            Assert.Null(target1);

            var target2 = service.GetById("AssetId2");
            Assert.Null(target2);
        }

        [Test]
        public async void DeleteOneAssetInARequestTest()
        {
            var service = new AssetService();

            var site = await AddSiteRequest();

            var asset = new Asset() { Id = "AssetId1", PortalId = site.Id, OwnerId = "AnOwnerId", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title1" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = "AnOwnerId", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);

            var idList = new List<string>();
            idList.Add("AssetId");
            var result = service.DeleteAssets(site.Id, idList);
            Assert.That(result, Is.EqualTo(0));

            var target2 = service.GetById("AssetId");
            Assert.NotNull(target2);
        }

        [Test]
        public async void CalculateCategoriesTest()
        {
            var service = new AssetService();
            var userManager = new PortalUserService(this.UserStore);

            var siteService = new SiteService();
            var site = new Site();
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = "AnOwnerId", Category = "Category 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = "AnOwnerId", Category = "Category 2", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);

            var target = service.CalculateCategories(site.Id);

            Assert.NotNull(target);
            Assert.That(target.Count, Is.EqualTo(2));
            Assert.That(target[0].Count, Is.EqualTo(1));
            Assert.That(target[0].Name, Is.EqualTo("Category 1"));
        }

        [Test]
        public async void CalculateCategories2SitesTest()
        {
            var service = new AssetService();
            var userManager = new PortalUserService(this.UserStore);

            var siteService = new SiteService();
            var site1 = new Site();
            siteService.Save(site1);

            var asset = new Asset() { Id = "AssetId", PortalId = site1.Id, OwnerId = "AnOwnerId", Category = "Category 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site1.Id, OwnerId = "AnOwnerId", Category = "Category 2", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);

            var site2 = new Site();
            siteService.Save(site2);

            var asset3 = new Asset() { Id = "AssetId3", PortalId = site2.Id, OwnerId = "AnOwnerId2", Category = "Category 3", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            service.Save(asset);

            var target = service.CalculateCategories(site1.Id);

            Assert.NotNull(target);
            Assert.That(target.Count, Is.EqualTo(2));
            Assert.That(target[0].Count, Is.EqualTo(1));
            Assert.That(target[0].Name, Is.EqualTo("Category 1"));
        }

        [Test]
        public async void CalculateManufacturersTest()
        {
            var service = new AssetService();
            var userManager = new PortalUserService(this.UserStore);

            var siteService = new SiteService();
            var site = new Site();
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = "AnOwnerId", Manufacturer = "Manu 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = "AnOwnerId", Manufacturer = "Manu 2", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);

            var target = service.CalculateManufacturers(site.Id);

            Assert.NotNull(target);
            Assert.That(target.Count, Is.EqualTo(2));
            Assert.That(target[0].Count, Is.EqualTo(1));
            Assert.That(target[0].Name, Is.EqualTo("Manu 1"));
        }

        [Test]
        public async void CalculateManufacturersOneNullTest()
        {
            var service = new AssetService();
            var userManager = new PortalUserService(this.UserStore);

            var siteService = new SiteService();
            var site = new Site();
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = "AnOwnerId", Manufacturer = "Manu 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = "AnOwnerId", Manufacturer = "Manu 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);
            var asset3 = new Asset() { Id = "AssetId3", PortalId = site.Id, OwnerId = "AnOwnerId", Manufacturer = null, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title3" };
            service.Save(asset3);

            var target = service.CalculateManufacturers(site.Id);

            Assert.NotNull(target);
            Assert.That(target.Count, Is.EqualTo(1));
            Assert.That(target[0].Count, Is.EqualTo(2));
            Assert.That(target[0].Name, Is.EqualTo("Manu 1"));
        }

        [Test]
        public async void CalculateManufacturers2SitesTest()
        {
            var service = new AssetService();
            var userManager = new PortalUserService(this.UserStore);

            var siteService = new SiteService();
            var site1 = new Site();
            siteService.Save(site1);

            var asset = new Asset() { Id = "AssetId", PortalId = site1.Id, OwnerId = "AnOwnerId", Manufacturer = "Manu 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site1.Id, OwnerId = "AnOwnerId", Manufacturer = "Manu 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);

            var site2 = new Site();
            siteService.Save(site2);

            var asset3 = new Asset() { Id = "AssetId3", PortalId = site2.Id, OwnerId = "AnOwnerId", Manufacturer = "Manu 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title3" };
            service.Save(asset3);

            var target = service.CalculateManufacturers(site1.Id);

            Assert.NotNull(target);
            Assert.That(target.Count, Is.EqualTo(1));
            Assert.That(target[0].Count, Is.EqualTo(2));
            Assert.That(target[0].Name, Is.EqualTo("Manu 1"));
        }

        [Test]
        public async void Delete1AssetTest()
        {
            var service = new AssetService();

            var siteService = new SiteService();
            var site1 = new Site();
            site1.SiteSettings.PortalTag = "Tag1";
            siteService.Save(site1);

            var site2 = new Site();
            site2.SiteSettings.PortalTag = "Tag2";
            siteService.Save(site2); 
            
            var asset = new Asset() { Id = "AssetId", PortalId = site1.Id, OwnerId = "AnOwnerId", Manufacturer = "Manu 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site2.Id, OwnerId = "AnOwnerId2", Manufacturer = "Manu 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);

            service.Delete(asset.Id);

            var target = service.Repository.All<Asset>().Where(x => x.PortalId == site1.Id).ToList();
            Assert.False(target.Any());

            target = service.Repository.All<Asset>().Where(x => x.PortalId == site2.Id).ToList();
            Assert.That(target.Count, Is.EqualTo(1));
        }

        [Test]
        public async void Delete1AssetUsingLinqTest()
        {
            var service = new AssetService();

            var siteService = new SiteService();
            var site1 = new Site();
            site1.SiteSettings.PortalTag = "Tag1";
            siteService.Save(site1);

            var site2 = new Site();
            site2.SiteSettings.PortalTag = "Tag2";
            siteService.Save(site2);

            var asset = new Asset() { Id = "AssetId", PortalId = site1.Id, OwnerId = "AnOwnerId", Manufacturer = "Manu 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            service.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site2.Id, OwnerId = "AnOwnerId2", Manufacturer = "Manu 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            service.Save(asset2);

            service.Repository.Delete<Asset>(x => x.PortalId == site1.Id);

            var target = service.Repository.All<Asset>().Where(x => x.PortalId == site1.Id).ToList();
            Assert.False(target.Any());

            target = service.Repository.All<Asset>().Where(x => x.PortalId == site2.Id).ToList();
            Assert.That(target.Count, Is.EqualTo(1));
        }
    
    }
}
