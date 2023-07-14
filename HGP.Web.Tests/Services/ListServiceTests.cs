using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;
using AspNet.Identity.MongoDB;
using AutoMapper;
using HGP.Common;
using HGP.Common.Database;
using HGP.Web.Controllers;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.List;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace HGP.Web.Tests.Services
{
    [TestFixture]
    public class ListServiceTests
    {
        #region Additional test attributes

        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        [OneTimeSetUp]
        public static void MyClassInitialize()
        {

            RegisterDependencies.InitMapper();
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

            var repo = new MongoRepository(WebConfigurationManager.AppSettings["MongoDbConnectionString"], WebConfigurationManager.AppSettings["MongoDbName"]);
            var collection = repo.GetQuery<Asset>();
            var result = collection.CreateIndex(IndexKeys.Text("Title", "Description", "Manufacturer", "ModelNumber", "SerialNumber", "HitNumber"));

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
        public void ListServiceConstructorTest()
        {
            var listService = IoC.Container.GetInstance<ListService>();
            Assert.IsNotNull(listService);
        }

        private static Random random = new Random();
        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private Asset CreateAsset(string siteId, string ownerId = "")
        {
            var asset = new Asset()
            {
                Id = "AssetId" + RandomString(6),
                PortalId = siteId,
                HitNumber = RandomString(6),
                OwnerId = ownerId,
                Category = "Category 1",
                Status = GlobalConstants.AssetStatusTypes.Available,
                Title = "Title" + RandomString(6),
                IsVisible = true,
                AvailForRedeploy = DateTime.Now,
                AvailForSale = DateTime.Now.AddDays(30)
            };

            return asset;
        }

        [Test]
        public void BuildListAssetsPage2Assets1HiddenTest()
        {
            var requestService = new RequestService();
            var assetService = new AssetService();
            var listService = new ListService(assetService, requestService);

            var siteService = new SiteService();
            var site = new Site();
            siteService.Save(site);

            var asset = CreateAsset(site.Id);
            assetService.Save(asset);
            var asset2 = CreateAsset(site.Id);
            asset2.IsVisible = false;
            assetService.Save(asset2);

            var result = listService.BuildListAssetsPage(site, "", "", asset.Title);
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ListHomeModelResults>(result);

            Assert.AreEqual(1, result.ResultCount);
            Assert.AreEqual(1, result.PagedAssets.Count);
            Assert.AreEqual(asset.Title, result.PagedAssets.First().Title);
        }

        [Test]
        public void BuildListAssetsPage2Assets2HiddenTest()
        {
            var requestService = new RequestService();
            var assetService = new AssetService();
            var listService = new ListService(assetService, requestService);

            var siteService = new SiteService();
            var site = new Site();
            siteService.Save(site);

            var asset = CreateAsset(site.Id);
            asset.IsVisible = false;
            assetService.Save(asset);
            var asset2 = CreateAsset(site.Id);
            asset2.IsVisible = false;
            assetService.Save(asset2);

            var result = listService.BuildListAssetsPage(site, "", "", asset.Title);
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ListHomeModelResults>(result);

            Assert.AreEqual(0, result.ResultCount);
            Assert.AreEqual(0, result.PagedAssets.Count);
        }

        [Test]
        public void BuildListAssetsPageSearch3AssetsTest()
        {
            var requestService = new RequestService();
            var assetService = new AssetService();
            var listService = new ListService(assetService, requestService);

            var siteService = new SiteService();
            var site = new Site();
            siteService.Save(site);

            var asset = CreateAsset(site.Id);
            assetService.Save(asset);
            var asset2 = CreateAsset(site.Id);
            assetService.Save(asset2);
            asset2 = CreateAsset(site.Id);
            asset2.IsVisible = false;
            assetService.Save(asset2);

            var result = listService.BuildListAssetsPage(site, "", "", asset.Title);
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ListHomeModelResults>(result);

            Assert.AreEqual(1, result.ResultCount);
            Assert.AreEqual(1,result.PagedAssets.Count);
            Assert.AreEqual(asset.Title, result.PagedAssets.First().Title);
        }

        [Test]
        public void BuildListAssetsPageSearch3Assets2SitesTest()
        {
            var requestService = new RequestService();
            var assetService = new AssetService();
            var listService = new ListService(assetService, requestService);

            var siteService = new SiteService();
            var site = new Site();
            siteService.Save(site);
            var site2 = new Site();
            siteService.Save(site2);

            var asset = CreateAsset(site.Id);
            assetService.Save(asset);
            var asset2 = CreateAsset(site2.Id);
            assetService.Save(asset2);
            asset2.IsVisible = false;
            assetService.Save(asset2);

            var result = listService.BuildListAssetsPage(site, "", "", asset.Title);
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ListHomeModelResults>(result);

            Assert.AreEqual(1, result.ResultCount);
            Assert.AreEqual(1, result.PagedAssets.Count);
            Assert.AreEqual(asset.Title, result.PagedAssets.First().Title);
        }

        [Test]
        public void BuildListAssetsPageList3Assets1HiddenTest()
        {
            var requestService = new RequestService();
            var assetService = new AssetService();
            var listService = new ListService(assetService, requestService);

            var siteService = new SiteService();
            var site = new Site();
            siteService.Save(site);

            var asset = CreateAsset(site.Id);
            assetService.Save(asset);
            var asset2 = CreateAsset(site.Id);
            assetService.Save(asset2);
            asset2 = CreateAsset(site.Id);
            asset2.IsVisible = false;
            assetService.Save(asset2);

            var result = listService.BuildListAssetsPage(site, "", "");
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ListHomeModelResults>(result);

            Assert.AreEqual(2, result.ResultCount);
            Assert.AreEqual(2, result.PagedAssets.Count);
        }

        private async Task<PortalUser> CreateUser(string siteId)
        {
            var user = new PortalUser() { PortalId = siteId, Email = "EmailAddress" + RandomString(6), UserName = "AUserName" + RandomString(6) };
            var userManager = new PortalUserService(this.UserStore);
            var userCreateResult = await userManager.CreateAsync(user, "123456");
            Assert.AreEqual(true, userCreateResult.Succeeded, String.Concat(userCreateResult.Errors));

            return user;
        }

        [Test]
        public async Task BuildListAssetsPageList3Assets1RequestedTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);
            var listService = new ListService(assetService, requestService);

            var site = new Site();
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var owner = CreateUser(site.Id).Result;
            var requestor = CreateUser(site.Id).Result;
            
            var asset1 = CreateAsset(site.Id, owner.Id);
            assetService.Save(asset1);
            var asset2 = CreateAsset(site.Id, owner.Id);
            assetService.Save(asset2);
            asset2 = CreateAsset(site.Id, owner.Id);
            asset2.IsVisible = false;
            assetService.Save(asset2);

            var request = requestService.GetOpenOrNewRequest(site.Id, owner.Id);
            request = requestService.AddToRequest(site.Id, requestor.Id, asset1.HitNumber);

            var result = listService.BuildListAssetsPage(site, "", "");
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ListHomeModelResults>(result);

            // At this point the assets are in the request cart, they are still visible
            Assert.AreEqual(2, result.ResultCount);
            Assert.AreEqual(2, result.PagedAssets.Count);

            // Submit cart for processing
            requestService.Process(site.Id, requestor.Id, request.Id);

            result = listService.BuildListAssetsPage(site, "", "");
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ListHomeModelResults>(result);

            // At this point the asset is requested and now hidden
            Assert.AreEqual(1, result.ResultCount);
            Assert.AreEqual(1, result.PagedAssets.Count);
        }

        [Test]
        public async Task BuildListAssetsPageList3Assets1Requested1DeniedTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);
            var listService = new ListService(assetService, requestService);

            var site = new Site();
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var owner = CreateUser(site.Id).Result;
            var requestor = CreateUser(site.Id).Result;

            var asset1 = CreateAsset(site.Id, owner.Id);
            assetService.Save(asset1);
            var asset2 = CreateAsset(site.Id, owner.Id);
            assetService.Save(asset2);
            asset2 = CreateAsset(site.Id, owner.Id);
            asset2.IsVisible = false;
            assetService.Save(asset2);

            var request = requestService.GetOpenOrNewRequest(site.Id, owner.Id);
            request = requestService.AddToRequest(site.Id, requestor.Id, asset1.HitNumber);

            // Submit cart for processing
            requestService.Process(site.Id, requestor.Id, request.Id);

            // Deny asset
            requestService.ProcessDecision(site.Id, owner.Id, request.AssetRequests.First().Id, request.Id, "denied", "Denied message");

            var result = listService.BuildListAssetsPage(site, "", "");
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ListHomeModelResults>(result);

            // At this point the asset is denied and back in the catalog again
            Assert.AreEqual(2, result.ResultCount);
            Assert.AreEqual(2, result.PagedAssets.Count);
        }

        [Test]
        public async Task BuildListAssetsPageList3Assets1Requested1ApprovedTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);
            var listService = new ListService(assetService, requestService);

            var site = new Site();
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var owner = CreateUser(site.Id).Result;
            var requestor = CreateUser(site.Id).Result;

            var asset1 = CreateAsset(site.Id, owner.Id);
            assetService.Save(asset1);
            var asset2 = CreateAsset(site.Id, owner.Id);
            assetService.Save(asset2);
            asset2 = CreateAsset(site.Id, owner.Id);
            asset2.IsVisible = false;
            assetService.Save(asset2);

            var request = requestService.GetOpenOrNewRequest(site.Id, owner.Id);
            request = requestService.AddToRequest(site.Id, requestor.Id, asset1.HitNumber);

            // Submit cart for processing
            requestService.Process(site.Id, requestor.Id, request.Id);

            // Deny asset
            requestService.ProcessDecision(site.Id, owner.Id, request.AssetRequests.First().Id, request.Id, "approved", "Approved message");

            var result = listService.BuildListAssetsPage(site, "", "");
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<ListHomeModelResults>(result);

            // At this point the asset is approved and out of the catalog
            Assert.AreEqual(1, result.ResultCount);
            Assert.AreEqual(1, result.PagedAssets.Count);
        }
    }
}