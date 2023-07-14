using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;
using AspNet.Identity.MongoDB;
using HGP.Common.Database;
using HGP.Web.Controllers;
using HGP.Web.DependencyResolution;
using HGP.Web.Models;
using HGP.Web.Services;
using MongoDB.Driver;
using NUnit.Framework;

namespace HGP.Web.Tests.Controllers
{
    [TestFixture]
    public class SettingsControllerTests
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
        public void ConstructorTest()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();
            Assert.IsNotNull(controller);
        }

        [Test]
        public void IsValidOneAddressTest()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();

            var target = controller.IsValid("@foo.com");
            Assert.True(target);
        }


        [Test]
        public void IsValidTwoAddressTest()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();

            var target = controller.IsValid("@foo.com,@hotmail.com.zh");
            Assert.True(target);
        }

        [Test]
        public void IsValidOneBadAddressTest()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();

            var target = controller.IsValid("@hotmail");
            Assert.False(target);
        }

        [Test]
        public void IsValidOneBadAddressTest2()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();

            var target = controller.IsValid("hotmailcom");
            Assert.False(target);
        }

        [Test]
        public void IsValidOneGoodOneBadAddressTest()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();

            var target = controller.IsValid("@foo.com,@hotmail");
            Assert.False(target);
        }

        [Test]
        public void EditEmailFilterNoAddressTest()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();
            Assert.IsNotNull(controller);
            var site = new Site { Id = "SiteId", SiteSettings = { PortalTag = "AdminPortalTag" } };
            IoC.Container.GetInstance<ISiteService>().Save(site);

            var result = controller.EditEmailFilter("", site.Id) as JsonResult;
            Assert.IsInstanceOf<JsonResult>(result);
            dynamic data = result.Data;
            bool target = data.success;
            Assert.True(target);
        }
        
        [Test]
        public void EditEmailFilterOneAddressTest()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();
            Assert.IsNotNull(controller);
            var site = new Site { Id = "SiteId", SiteSettings = { PortalTag = "AdminPortalTag" } };
            IoC.Container.GetInstance<ISiteService>().Save(site);

            var result = controller.EditEmailFilter("@foo.com", site.Id) as JsonResult;
            Assert.IsInstanceOf<JsonResult>(result);
            dynamic data = result.Data;
            bool target = data.success;
            Assert.True(target);
        }

        [Test]
        public void EditEmailFilterTwoAddressesTest()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();
            Assert.IsNotNull(controller);
            var site = new Site { Id = "SiteId", SiteSettings = { PortalTag = "AdminPortalTag" } };
            IoC.Container.GetInstance<ISiteService>().Save(site);

            var result = controller.EditEmailFilter("@foo.com,@hotmail.nl", site.Id) as JsonResult;
            Assert.IsInstanceOf<JsonResult>(result);
            dynamic data = result.Data;
            bool target = data.success;
            Assert.True(target);
        }

        [Test]
        public void EditEmailFilterTwoAddressesOneBadTest()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();
            Assert.IsNotNull(controller);
            var site = new Site { Id = "SiteId", SiteSettings = { PortalTag = "AdminPortalTag" } };
            IoC.Container.GetInstance<ISiteService>().Save(site);

            var result = controller.EditEmailFilter("@foo.com,@hotmail", site.Id) as JsonResult;
            Assert.IsInstanceOf<JsonResult>(result);
            dynamic data = result.Data;
            bool target = data.success;
            Assert.False(target);
        }

        [Test]
        public void EditEmailFilterTwoAddressesWithSpacesTest()
        {
            var controller = IoC.Container.GetInstance<SettingsController>();
            Assert.IsNotNull(controller);
            var site = new Site { Id = "SiteId", SiteSettings = { PortalTag = "AdminPortalTag" } };
            IoC.Container.GetInstance<ISiteService>().Save(site);

            var result = controller.EditEmailFilter(" @foo.com , @hotmail.nl ", site.Id) as JsonResult;
            Assert.IsInstanceOf<JsonResult>(result);
            dynamic data = result.Data;
            bool target = data.success;
            Assert.True(target);
        }

    }
}