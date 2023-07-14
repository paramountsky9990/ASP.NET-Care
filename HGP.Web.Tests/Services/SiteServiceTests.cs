using System;
using System.IO;
using System.Web.Configuration;
using AspNet.Identity.MongoDB;
using HGP.Common.Database;
using HGP.Web.Controllers;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Services;
using MongoDB.Driver;
using NUnit.Framework;

namespace HGP.Web.Tests.Services
{
    [TestFixture]
    public class SiteServiceTests
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
        public void SiteServiceConstructorTest()
        {
            var service = new SiteService();
            Assert.IsNotNull(service);
        }

        [Test]
        public async void CreateBucketTest()
        {
            var site = new Site { SiteSettings = { PortalTag = "APortalTag" } };
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            var service = new SiteService();
            service.CreateBucket(site);

            var exists = new AwsService().BucketExists("APortalTag");
            Assert.IsTrue(exists);
        }





    }
}

