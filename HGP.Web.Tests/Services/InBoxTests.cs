using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using AspNet.Identity.MongoDB;
using HGP.Common;
using HGP.Common.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Services;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace HGP.Web.Tests.Services
{
    [TestFixture]
    public class InBoxTests
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
        public void InBoxServiceConstructorTest()
        {
            var requestService = IoC.Container.GetInstance<IRequestService>();
            var service = new InBoxService(requestService);
            Assert.IsNotNull(service);
        }

        [Test]
        public async void NoPendingRequestsCountTest()
        {
            var requestService = new RequestService();
            var inBoxService = new InBoxService(requestService);
            var userManager = new PortalUserService(this.UserStore);

            var siteService = new SiteService();
            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var owner = new PortalUser() { PortalId = site.Id, Email = "EmailAddress1", UserName = "AUserName1", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(owner, "123456");
            Assert.True(result.Succeeded);
            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress2", UserName = "AUserName2", Address = { Street1 = "Street2" } };
            result = await userManager.CreateAsync(requestor, "123456");
            Assert.True(result.Succeeded);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = owner.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            requestService.AddToRequest(asset, requestor);

            var target = inBoxService.BuildInBoxHomeModel(site.Id, owner.Id);
            Assert.NotNull(target);
            Assert.That(target.InBoxCount, Is.EqualTo(0));
        }

        [Test]
        public async void PendingRequestsCountTest()
        {
            var requestService = new RequestService();
            var inBoxService = new InBoxService(requestService);
            var userManager = new PortalUserService(this.UserStore);

            var siteService = new SiteService();
            var site = new Site();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            var owner = new PortalUser() { PortalId = site.Id, Email = "EmailAddress1", UserName = "AUserName1", Address = { Street1 = "Street1" } };
            var result = await userManager.CreateAsync(owner, "123456");
            Assert.True(result.Succeeded);
            var requestor = new PortalUser() { PortalId = site.Id, Email = "EmailAddress2", UserName = "AUserName2", Address = { Street1 = "Street2" } };
            result = await userManager.CreateAsync(requestor, "123456");
            Assert.True(result.Succeeded);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = owner.Id, Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            var request = requestService.AddToRequest(asset, requestor);
            requestService.SetAssetRequestStatus(request, "AssetId", GlobalConstants.RequestStatusTypes.Pending);
            requestService.Save(request);

            var target = inBoxService.BuildInBoxHomeModel(site.Id, owner.Id);
            Assert.NotNull(target);
            Assert.That(target.InBoxCount, Is.EqualTo(1));
        }

    }
}
