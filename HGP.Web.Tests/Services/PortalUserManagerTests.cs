using System;
using System.Linq;
using System.Web.Configuration;
using AspNet.Identity.MongoDB;
using HGP.Common.Database;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using MongoDB.Driver;
using NUnit.Framework;

namespace HGP.Web.Tests.Services
{
    [TestFixture]
    public class PortalUserManagerTests
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
        }

        //Use ClassCleanup to run code after all tests in a class have run
        [OneTimeTearDown]
        public static void MyClassCleanup()
        {
            var repository = IoC.Container.GetInstance<IMongoRepository>();
            repository.AllowDatabaseDrop = true;
            repository.DropDatabase();

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
        public void PortalUserManagerConstructorTest()
        {
            var userManager = new PortalUserService(this.UserStore);
            Assert.IsNotNull(userManager);
        }

        [Test]
        public async void AddUserToSiteTest()
        {
            var userManager = new PortalUserService(this.UserStore);
            Assert.IsNotNull(userManager);

            var s = IoC.Container.GetInstance<IWorkContext>().S;
            Assert.IsNotNull(s);

            var site = new Site { SiteSettings = { } };
            s.SiteService.Save(site);

            var user = new PortalUser() {UserName = "rrb@matrix6.com", Email = "rrb@matrix6.com"};
            var result = await userManager.CreateAsync(user, "gamma12");
            Assert.AreEqual(result.Succeeded, true);
            await userManager.AddUserToSite(user.Id, site);
            Assert.AreEqual(userManager.GetIds(site.Id).Count, 1);

            var targetUser = userManager.FindById(user.Id);
            Assert.AreEqual(targetUser.PortalId, site.Id);

        }

        [Test]
        public async void DeleteUserFromSiteTest()
        {
            var userManager = new PortalUserService(this.UserStore);
            Assert.IsNotNull(userManager);

            var s = IoC.Container.GetInstance<IWorkContext>().S;
            Assert.IsNotNull(s);

            var site = new Site { SiteSettings = { } };
            s.SiteService.Save(site);

            var user = new PortalUser() { UserName = "rrb@matrix6.com", Email = "rrb@matrix6.com" };
            var result = await userManager.CreateAsync(user, "gamma12");
            Assert.AreEqual(result.Succeeded, true);
            await userManager.AddUserToSite(user.Id, site);
            Assert.AreEqual(userManager.GetIds(site.Id).Count, 1);

            var targetUser = userManager.FindById(user.Id);
            Assert.AreEqual(targetUser.PortalId, site.Id);

            // Now delete it
            string[] list = new string[1];
            list[0] = targetUser.Id;

            userManager.DeleteFromSite(site.Id, list);

            Assert.AreEqual(userManager.GetIds(site.Id).Count, 0);


        }

    }
}
