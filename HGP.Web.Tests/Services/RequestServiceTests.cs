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
    public class RequestServiceTests
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

        #region Adding to cart tests
        [Test]
        public async Task AddToRequest1Asset1UserTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor, "123456");
            Assert.True(result.Succeeded);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);
            
            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            requestService.AddToRequest(asset, requestor);

            var target = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Open).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.RequestorId, Is.EqualTo(requestor.Id));
            Assert.That(target.AssetRequests.Count, Is.EqualTo(1));
            Assert.That(target.AssetRequests[0].Title, Is.EqualTo(asset.Title));
            Assert.That(target.ShipToAddress.Street1, Is.EqualTo(requestor.Address.Street1));
        }

        [Test]
        public async Task AddToRequest1Asset1User2RequestsTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor, "123456");
            Assert.True(result.Succeeded);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            requestService.AddToRequest(asset, requestor);

            requestService.AddToRequest(asset, requestor);

            var target = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Open).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.RequestorId, Is.EqualTo(requestor.Id));
            Assert.That(target.AssetRequests.Count, Is.EqualTo(1));
            Assert.That(target.AssetRequests[0].Title, Is.EqualTo(asset.Title));
            Assert.That(target.ShipToAddress.Street1, Is.EqualTo(requestor.Address.Street1));
        }

        [Test]
        public async Task Add2Assets1UserToRequestTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);
            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName" };
            var result = await userManager.CreateAsync(requestor, "123456");

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            requestService.AddToRequest(asset, requestor);

            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            requestService.AddToRequest(asset2, requestor);


            var target = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Open).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.RequestorId, Is.EqualTo(requestor.Id));
            Assert.That(target.AssetRequests.Count, Is.EqualTo(2));
            Assert.That(target.AssetRequests[0].Title, Is.EqualTo(asset.Title));
            Assert.That(target.AssetRequests[1].Title, Is.EqualTo(asset2.Title));
        }

        [Test]
        public async Task AddToRequest2Assets2UsersTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService); 
            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var requestor1 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress1", UserName = "AUserName1" };
            var result1 = await userManager.CreateAsync(requestor1, "123456");
            var requestor2 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress2", UserName = "AUserName2" };
            var result2 = await userManager.CreateAsync(requestor2, "123456");

            site.Locations.First().OwnerId = requestor1.Id;
            siteService.Save(site); 
            
            var asset1 = new Asset() { Id = "AssetId1", PortalId = site.Id, OwnerId = requestor1.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title1" };
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = requestor2.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };

            requestService.AddToRequest(asset1, requestor1);
            requestService.AddToRequest(asset2, requestor2);

            var target = requestService.GetByUserIdStatus(site.Id, requestor1.Id, GlobalConstants.RequestStatusTypes.Open);
            Assert.NotNull(target);
            Assert.That(target.Count, Is.EqualTo(1));
            Assert.NotNull(target.FirstOrDefault());
            Assert.That(target[0].RequestorId, Is.EqualTo(requestor1.Id));
            Assert.That(target[0].AssetRequests.Count, Is.EqualTo(1));
            Assert.That(target[0].AssetRequests[0].Title, Is.EqualTo(asset1.Title));
        }

        [Test]
        public async Task AddRequest1Asset1User2RequestsAssetTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService); 
            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var requestor1 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress1", UserName = "AUserName1" };
            var result1 = await userManager.CreateAsync(requestor1, "123456");

            site.Locations.First().OwnerId = requestor1.Id;
            siteService.Save(site); 
            
            var asset1 = new Asset() { Id = "AssetId1", PortalId = site.Id, OwnerId = requestor1.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title1" };

            requestService.AddToRequest(asset1, requestor1);
            requestService.AddToRequest(asset1, requestor1);

            var target = requestService.GetByUserIdStatus(site.Id, requestor1.Id, GlobalConstants.RequestStatusTypes.Open);
            Assert.NotNull(target);
            Assert.That(target.Count, Is.EqualTo(1));
            Assert.NotNull(target.FirstOrDefault());
            Assert.That(target[0].RequestorId, Is.EqualTo(requestor1.Id));
            Assert.That(target[0].AssetRequests.Count, Is.EqualTo(1));
            Assert.That(target[0].AssetRequests[0].Title, Is.EqualTo(asset1.Title));
        }

        [Test]
        public async Task RemoveFromRequestTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService); 
            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor, "123456");
            Assert.True(result.Succeeded);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            requestService.AddToRequest(asset, requestor);

            var request = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Open).FirstOrDefault();
            requestService.RemoveFromRequest(site.Id, requestor.Id, request.AssetRequests.FirstOrDefault().Id);

            request = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Open).FirstOrDefault();
            Assert.That(request, Is.Null);
        }

        [Test]
        public async Task Remove1AssetFromRequestTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService); 
            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName" };
            var result = await userManager.CreateAsync(requestor, "123456");

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            requestService.AddToRequest(asset, requestor);

            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            requestService.AddToRequest(asset2, requestor);


            var request = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Open).FirstOrDefault();
            requestService.RemoveFromRequest(site.Id, requestor.Id, request.AssetRequests.FirstOrDefault().Id);

            request = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Open).FirstOrDefault();
            Assert.NotNull(request);
            Assert.That(request.RequestorId, Is.EqualTo(requestor.Id));
            Assert.That(request.AssetRequests.Count, Is.EqualTo(1));
            Assert.That(request.AssetRequests[0].Title, Is.EqualTo(asset2.Title));
            Assert.That(request.AssetCount, Is.EqualTo(1));
        }
        #endregion

        #region Submitting cart tests
        [Test]
        public async Task ProcessRequest1Asset1UserTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor, "123456");
            Assert.True(result.Succeeded);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", HitNumber= "HitNumber", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            assetService.Save(asset);
            var request = requestService.AddToRequest(asset, requestor);

            requestService.Process(site.Id, requestor.Id, request.Id);

            // Asset will be in pending and no longer visible
            var target = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Pending).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.AssetRequests.Count, Is.EqualTo(1));
            Assert.That(target.Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Pending));

            asset = assetService.GetByHitNumber(site.Id, "HitNumber");
            Assert.NotNull(asset);
            Assert.That(asset.IsVisible, Is.False);

            site = siteService.GetById(site.Id);
            Assert.That(site.Categories.Any, Is.False); // Category list is empty
        }

        [Test]
        public async Task Process1Request1Asset_AllowMultipleRequests_UserTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            // Enable multiple requests per asset
            site.SiteSettings.Features.Add("allowmultiplerequests");
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor, "123456");
            Assert.True(result.Succeeded);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", HitNumber = "HitNumber", PortalId = site.Id, OwnerId = requestor.Id, IsVisible = true, Category= "Category1", Status = GlobalConstants.AssetStatusTypes.Available, 
                                        AvailForSale = DateTime.Now.AddDays(1), Title = "A Title" };
            assetService.Save(asset);
            var request = requestService.AddToRequest(asset, requestor);

            requestService.Process(site.Id, requestor.Id, request.Id);

            // Asset will be in pending and IS visible
            var target = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Pending).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.AssetRequests.Count, Is.EqualTo(1));
            Assert.That(target.Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Pending));

            asset = assetService.GetByHitNumber(site.Id, "HitNumber");
            Assert.NotNull(asset);
            Assert.That(asset.IsVisible, Is.True);

            site = siteService.GetById(site.Id);
            Assert.That(site.Categories.First().Count, Is.EqualTo(1)); // Category list is 1
        }

        [Test]
        public async Task Process2Requests1Asset_AllowMultipleRequests_UserTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            // Enable multiple requests per asset
            site.SiteSettings.Features.Add("allowmultiplerequests");
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var requestor1 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor1, "123456");
            Assert.True(result.Succeeded);

            var requestor2 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress2", UserName = "AUserName2", Address = { Street1 = "Street1" } };
            result = await userManager.CreateAsync(requestor2, "123456");
            Assert.True(result.Succeeded);
            
            site.Locations.First().OwnerId = requestor1.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", HitNumber = "HitNumber", PortalId = site.Id, OwnerId = requestor1.Id, IsVisible = true, Category= "Category1", Status = GlobalConstants.AssetStatusTypes.Available, 
                                        AvailForSale = DateTime.Now.AddDays(1), Title = "A Title" };
            assetService.Save(asset);
            var request1 = requestService.AddToRequest(asset, requestor1);
            var request2 = requestService.AddToRequest(asset, requestor2);

            requestService.Process(site.Id, requestor1.Id, request1.Id);
            requestService.Process(site.Id, requestor2.Id, request2.Id);

            // Asset will be in pending and IS visible
            var target = requestService.GetByUserIdStatus(site.Id, requestor1.Id, GlobalConstants.RequestStatusTypes.Pending).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.AssetRequests.Count, Is.EqualTo(1));
            Assert.That(target.Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Pending));

            asset = assetService.GetByHitNumber(site.Id, "HitNumber");
            Assert.NotNull(asset);
            Assert.That(asset.IsVisible, Is.True);

            site = siteService.GetById(site.Id);
            Assert.That(site.Categories.First().Count, Is.EqualTo(1)); // Category list is 1
        }

        #endregion

        #region Approval/denial tests
        [Test]
        public async Task ApprovalRequest1Asset1UserTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor, "123456");
            Assert.True(result.Succeeded);

            var approver = new PortalUser() { PortalId = site.Id, Email = "EmailAddress2", UserName = "AUserName2", Address = { Street1 = "Street1" } };
            result = await userManager.CreateAsync(approver, "123456");
            Assert.True(result.Succeeded);
            
            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", HitNumber = "HitNumber", PortalId = site.Id, OwnerId = requestor.Id, IsVisible = true, Category= "Category1", Status = GlobalConstants.AssetStatusTypes.Available, 
                                        AvailForSale = DateTime.Now.AddDays(1), Title = "A Title" };
            assetService.Save(asset);
            siteService.UpdateCategories(site.Id);
            var request = requestService.AddToRequest(asset, requestor);

            requestService.Process(site.Id, requestor.Id, request.Id);

            requestService.ProcessDecision(site.Id, approver.Id, request.AssetRequests.First().Id, request.Id, "approved", "Approval message");

            // Asset will be in pending and no longer visible
            var target = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Completed).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.AssetRequests.Count, Is.EqualTo(1));
            Assert.That(target.Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Completed));
            Assert.That(target.AssetRequests.First().Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Approved));

            asset = assetService.GetByHitNumber(site.Id, "HitNumber");
            Assert.NotNull(asset);
            Assert.That(asset.IsVisible, Is.False);
            Assert.That(asset.Status, Is.EqualTo(GlobalConstants.AssetStatusTypes.Requested));

            site = siteService.GetById(site.Id);
            Assert.That(site.Categories.Any, Is.False); // Category list is empty
        }

        [Test]
        public async Task DenialRequest1Asset1UserTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor, "123456");
            Assert.True(result.Succeeded);

            var approver = new PortalUser() { PortalId = site.Id, Email = "EmailAddress2", UserName = "AUserName2", Address = { Street1 = "Street1" } };
            result = await userManager.CreateAsync(approver, "123456");
            Assert.True(result.Succeeded);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", HitNumber = "HitNumber", PortalId = site.Id, OwnerId = requestor.Id, IsVisible = true, Category= "Category1", Status = GlobalConstants.AssetStatusTypes.Available, 
                                        AvailForSale = DateTime.Now.AddDays(1), Title = "A Title" };
            assetService.Save(asset);
            siteService.UpdateCategories(site.Id);
            var request = requestService.AddToRequest(asset, requestor);

            requestService.Process(site.Id, requestor.Id, request.Id);

            requestService.ProcessDecision(site.Id, approver.Id, request.AssetRequests.First().Id, request.Id, "denied", "Denied message");

            // Asset will be in pending and no longer visible
            var target = requestService.GetByUserIdStatus(site.Id, requestor.Id, GlobalConstants.RequestStatusTypes.Completed).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.AssetRequests.Count, Is.EqualTo(1));
            Assert.That(target.Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Completed));
            Assert.That(target.AssetRequests.First().Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Denied));

            asset = assetService.GetByHitNumber(site.Id, "HitNumber");
            Assert.NotNull(asset);
            Assert.That(asset.IsVisible, Is.True);
            Assert.That(asset.Status, Is.EqualTo(GlobalConstants.AssetStatusTypes.Available));

            site = siteService.GetById(site.Id);
            Assert.That(site.Categories.First().Count, Is.EqualTo(1));
        }


        [Test]
        public async Task Approval2Requests1Asset_AllowMultipleRequests_UserTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            // Enable multiple requests per asset
            site.SiteSettings.Features.Add("allowmultiplerequests");
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var requestor1 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor1, "123456");
            Assert.True(result.Succeeded);

            var requestor2 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress2", UserName = "AUserName2", Address = { Street1 = "Street1" } };
            result = await userManager.CreateAsync(requestor2, "123456");
            Assert.True(result.Succeeded);

            var approver = new PortalUser() { PortalId = site.Id, Email = "EmailAddress3", UserName = "AUserName3", Address = { Street1 = "Street1" } };
            result = await userManager.CreateAsync(approver, "123456");
            Assert.True(result.Succeeded);

            site.Locations.First().OwnerId = requestor1.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", HitNumber = "HitNumber", PortalId = site.Id, OwnerId = requestor1.Id, IsVisible = true, Category= "Category1", Status = GlobalConstants.AssetStatusTypes.Available, 
                                        AvailForSale = DateTime.Now.AddDays(1), Title = "A Title" };
            assetService.Save(asset);
            var request1 = requestService.AddToRequest(asset, requestor1);
            var request2 = requestService.AddToRequest(asset, requestor2);

            requestService.Process(site.Id, requestor1.Id, request1.Id);
            requestService.Process(site.Id, requestor2.Id, request2.Id);

            requestService.ProcessDecision(site.Id, approver.Id, request1.AssetRequests.First().Id, request1.Id, "approved");

            // First request will be approved, second will be denied, asset will be hidden
            var target = requestService.GetByUserIdStatus(site.Id, requestor1.Id, GlobalConstants.RequestStatusTypes.Completed).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.AssetRequests.First().Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Approved));
            Assert.That(target.Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Completed));

            target = requestService.GetByUserIdStatus(site.Id, requestor2.Id, GlobalConstants.RequestStatusTypes.Completed).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.AssetRequests.First().Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Denied));
            Assert.That(target.Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Completed));

            asset = assetService.GetByHitNumber(site.Id, "HitNumber");
            Assert.NotNull(asset);
            Assert.That(asset.IsVisible, Is.False);
        }

        [Test]
        public async Task Approval2Requests1Asset_AllowMultipleRequests_RequestOrderReversed_UserTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            // Enable multiple requests per asset
            site.SiteSettings.Features.Add("allowmultiplerequests");
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var requestor1 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor1, "123456");
            Assert.True(result.Succeeded);

            var requestor2 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress2", UserName = "AUserName2", Address = { Street1 = "Street1" } };
            result = await userManager.CreateAsync(requestor2, "123456");
            Assert.True(result.Succeeded);

            var approver = new PortalUser() { PortalId = site.Id, Email = "EmailAddress3", UserName = "AUserName3", Address = { Street1 = "Street1" } };
            result = await userManager.CreateAsync(approver, "123456");
            Assert.True(result.Succeeded);

            site.Locations.First().OwnerId = requestor1.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", HitNumber = "HitNumber", PortalId = site.Id, OwnerId = requestor1.Id, IsVisible = true, Category= "Category1", Status = GlobalConstants.AssetStatusTypes.Available, 
                                        AvailForSale = DateTime.Now.AddDays(1), Title = "A Title" };
            assetService.Save(asset);
            var request1 = requestService.AddToRequest(asset, requestor1);
            var request2 = requestService.AddToRequest(asset, requestor2);

            requestService.Process(site.Id, requestor1.Id, request1.Id);
            requestService.Process(site.Id, requestor2.Id, request2.Id);

            requestService.ProcessDecision(site.Id, approver.Id, request2.AssetRequests.First().Id, request2.Id, "approved");

            // First request will be approved, second will be denied, asset will be hidden
            var target = requestService.GetByUserIdStatus(site.Id, requestor1.Id, GlobalConstants.RequestStatusTypes.Completed).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.AssetRequests.First().Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Denied));
            Assert.That(target.Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Completed));

            target = requestService.GetByUserIdStatus(site.Id, requestor2.Id, GlobalConstants.RequestStatusTypes.Completed).FirstOrDefault();
            Assert.NotNull(target);
            Assert.That(target.AssetRequests.First().Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Approved));
            Assert.That(target.Status, Is.EqualTo(GlobalConstants.RequestStatusTypes.Completed));

            asset = assetService.GetByHitNumber(site.Id, "HitNumber");
            Assert.NotNull(asset);
            Assert.That(asset.IsVisible, Is.False);
        }
        #endregion

        #region Misc tests
        [Test]
        public async Task IncrementRequestNumberTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService); 
            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            siteService.Save(site);
            var targetRequestNumber = site.SiteSettings.NextRequestNum.ToString();

            var requestor1 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress1", UserName = "AUserName1" };
            var result1 = await userManager.CreateAsync(requestor1, "123456");

            var target = requestService.GetOpenOrNewRequest(site.Id, requestor1.Id);
            Assert.NotNull(target);
            Assert.That(int.Parse(target.RequestNum), Is.EqualTo(int.Parse(targetRequestNumber) + 1));

            var targetSite = siteService.GetById(site.Id);
            Assert.That(targetSite.SiteSettings.NextRequestNum, Is.EqualTo(int.Parse(targetRequestNumber) + 1));

        }

        [Test]
        public async Task GetRequestedAssetIdsTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName" };
            var result = await userManager.CreateAsync(requestor, "123456");

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            var request = requestService.AddToRequest(asset, requestor);

            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = requestor.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            request = requestService.AddToRequest(asset2, requestor);

            requestService.SetAssetRequestStatus(request, "AssetId", GlobalConstants.RequestStatusTypes.Open);
            requestService.SetAssetRequestStatus(request, "AssetId2", GlobalConstants.RequestStatusTypes.Pending);
            requestService.Save(request);

            var target = requestService.GetRequestedAssetIds(site.Id);
            Assert.NotNull(target);
            Assert.That(target.Count, Is.EqualTo(1));
            Assert.That(target[0], Is.EqualTo("AssetId2"));
        }

        [Test]
        public async Task GetByAssetId_1AssetTest()
        {
            var assetService = new AssetService();
            var siteService = new SiteService();
            var requestService = new RequestService(assetService, new Web.Services.EmailService(), siteService);

            var userManager = new PortalUserService(this.UserStore);

            var site = new Site();
            // Enable multiple requests per asset
            site.SiteSettings.Features.Add("allowmultiplerequests");
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;

            var requestor1 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress", UserName = "AUserName", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(requestor1, "123456");
            Assert.True(result.Succeeded);

            var requestor2 = new PortalUser() { PortalId = site.Id, Email = "EmailAddress2", UserName = "AUserName2", Address = { Street1 = "Street1" } };
            result = await userManager.CreateAsync(requestor2, "123456");
            Assert.True(result.Succeeded);
            
            site.Locations.First().OwnerId = requestor1.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", HitNumber = "HitNumber", PortalId = site.Id, OwnerId = requestor1.Id, IsVisible = true, Category= "Category1", Status = GlobalConstants.AssetStatusTypes.Available, 
                                        AvailForSale = DateTime.Now.AddDays(1), Title = "A Title" };
            assetService.Save(asset);
            var request1 = requestService.AddToRequest(asset, requestor1);
            var request2 = requestService.AddToRequest(asset, requestor2);

            var asset2 = new Asset() { Id = "AssetId2", HitNumber = "HitNumber2", PortalId = site.Id, OwnerId = requestor1.Id, IsVisible = true, Category= "Category1", Status = GlobalConstants.AssetStatusTypes.Available, 
                                        AvailForSale = DateTime.Now.AddDays(1), Title = "A Title" };
            assetService.Save(asset);
            var request3 = requestService.AddToRequest(asset2, requestor1);


            requestService.Process(site.Id, requestor1.Id, request1.Id);
            requestService.Process(site.Id, requestor2.Id, request2.Id);


            var target = requestService.GetByAssetId(site.Id, "AssetId");
            Assert.NotNull(target);
            Assert.That(target.Count(), Is.EqualTo(2));
            Assert.That(target.First().AssetRequests.First().Id, Is.EqualTo(asset.Id));
        }
        #endregion
    }
}
