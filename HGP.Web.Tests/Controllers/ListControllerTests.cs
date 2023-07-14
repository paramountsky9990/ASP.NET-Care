using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;
using AspNet.Identity.MongoDB;
using HGP.Common;
using HGP.Common.Database;
using HGP.Web.Controllers;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Services;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace HGP.Web.Tests.Controllers
{
    [TestFixture]
    public class ListControllerTests
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
        [OneTimeSetUp]
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

            var repo = new MongoRepository(WebConfigurationManager.AppSettings["MongoDbConnectionString"], WebConfigurationManager.AppSettings["MongoDbName"]);
            var collection = repo.GetQuery<Asset>();
            var result = collection.CreateIndex(IndexKeys.Text("Title", "Description", "Manufacturer", "ModelNumber", "SerialNumber", "HitNumber"));
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
        public void AdminSiteControllerConstructorTest()
        {
            var listController = IoC.Container.GetInstance<ListController>();
            Assert.IsNotNull(listController);
        }

        [Test]
        public void Search1AssetTest()
        {
            var assetService = new AssetService();
            var userManager = new PortalUserService(this.UserStore);
            var user = new PortalUser() { UserName = "rrb@matrix6.com", Email = "rrb@matrix6.com" };
            userManager.CreateAsync(user, "gamma12");
            IoC.Container.GetInstance<IWorkContext>().CurrentUser = user;

            var siteService = new SiteService();
            var site = new Site();
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
       

            var asset = new Asset() { Id = "AssetId", PortalId = site.Id, OwnerId = "AnOwnerId", Category = "Category 1", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title" };
            assetService.Save(asset);
            var asset2 = new Asset() { Id = "AssetId2", PortalId = site.Id, OwnerId = "AnOwnerId", Category = "Category 2", Status = GlobalConstants.AssetStatusTypes.Available, Title = "A Title2" };
            assetService.Save(asset2);

            var listController = IoC.Container.GetInstance<ListController>();
            var result = listController.Index("", "Title");
            Assert.IsInstanceOf<ViewResult>(result);
        }
    }
}