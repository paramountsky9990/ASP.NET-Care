using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using AspNet.Identity.MongoDB;
using HGP.Common.Database;
using HGP.Web.Controllers;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using MongoDB.Driver;
using NUnit.Framework;
using StructureMap;
using StructureMap.Web;

namespace HGP.Web.Tests.Controllers
{
    [TestFixture]
    public class AdminSiteControllerTest
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

            var userManager = IoC.Container.GetInstance<PortalUserService>();



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


            //var client = new MongoClient(WebConfigurationManager.AppSettings["MongoDbConnectionString"]);
            //    //todo: Move to config file
            //var database = client.GetServer().GetDatabase("CareDbTests");
            //var users = database.GetCollection<IdentityUser>("PortalUsers");
            //var roles = database.GetCollection<IdentityRole>("Roles");
            //this.UserStore = new UserStore<PortalUser>(new ApplicationIdentityContext(users, roles));
            //var userManager = new PortalUserService(this.UserStore);

            //Container.Configure(x => x.For<PortalUserService>().HybridHttpOrThreadLocalScoped().Use(() => userManager ));
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
            var siteController = IoC.Container.GetInstance<AdminSiteController>();
            //var siteController = new AdminSiteController(new PortalServices(IoC.Container.GetInstance<AssetService>(), IoC.Container.GetInstance<SiteService>(), IoC.Container.GetInstance<IWorkContext>()));
            Assert.IsNotNull(siteController);
        }

        [Test]
        public async void CreateWithModelTest()
        {
            var site = new Site { Id = "SiteId", SiteSettings = { PortalTag = "AdminPortalTag" } };
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            IoC.Container.GetInstance<ISiteService>().Save(site);
            var user = new PortalUser();
            IoC.Container.GetInstance<IWorkContext>().CurrentUser = user;
            
            var siteController = IoC.Container.GetInstance<AdminSiteController>();
            //var siteController = new AdminSiteController(new PortalServices(IoC.Container.GetInstance<AssetService>(), IoC.Container.GetInstance<SiteService>(), IoC.Container.GetInstance<IWorkContext>()));
            Assert.IsNotNull(siteController);

            var s = IoC.Container.GetInstance<IWorkContext>().S;
            Assert.IsNotNull(s);

            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<SiteCreateModel>();
            model.CompanyName = "ACompanyName";
            model.PortalTag = "APortalTag";

            var result = siteController.Create(model);
            Assert.IsInstanceOf<RedirectToRouteResult>(result);

            var targetSite = s.SiteService.GetByPortalTag("APortalTag");
            Assert.IsNotNull(targetSite);
            Assert.AreEqual(targetSite.SiteSettings.CompanyName, "ACompanyName");
            Assert.AreEqual(targetSite.SiteSettings.PortalTag, "APortalTag");

        }

    }
}

