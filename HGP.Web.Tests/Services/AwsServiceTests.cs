using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web.Configuration;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime;
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
using Amazon;
using Amazon.Runtime.Internal.Util;

namespace HGP.Web.Tests.Services
{
    [TestFixture]
    public class AwsServiceTests
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
        public void AwsServiceConstructorTest()
        {
            var service = new AwsService();
            Assert.IsNotNull(service);
        }

        [Test]
        public async void TryCreateBucketTest()
        {
            var service = new AwsService();
            var target = service.TryCreateBucket("ABucket");

            var exists = service.BucketExists("ABucket");
            Assert.IsTrue(exists);
        }

        [Test]
        public async Task PutFileTest()
        {
            var site = new Site {SiteSettings = {PortalTag = "APortalTag"}};
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            var service = new AwsService();

            var fileData = new FileStream(@"..\..\TestData\2073372.jpg", FileMode.Open);
            var memoryStream = new MemoryStream();
            using (var br = new BinaryReader(fileData))
                memoryStream.Write(br.ReadBytes((int)fileData.Length), 0, (int)fileData.Length);

            AWSConfigs.LoggingConfig.LogTo = LoggingOptions.Console;
            Debug.WriteLine("Hello");
            service.PutFile(site.SiteSettings.PortalTag, @"i", "AnImage.jpg", "image/jpeg", memoryStream);
            var exists = service.FileExists(site.SiteSettings.PortalTag + "/i/AnImage.jpg");
            Assert.IsTrue(exists);

        }

        [Test]
        public async Task DeleteFileTest()
        {
            var site = new Site { SiteSettings = { PortalTag = "APortalTag" } };
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            var service = new AwsService();

            var fileData = new FileStream(@"..\..\TestData\2073372.jpg", FileMode.Open);
            var memoryStream = new MemoryStream();
            using (var br = new BinaryReader(fileData))
                memoryStream.Write(br.ReadBytes((int)fileData.Length), 0, (int)fileData.Length);
            service.PutFile(site.SiteSettings.PortalTag, @"i", "AnImage.jpg", "image/jpeg", memoryStream);

            fileData = new FileStream(@"..\..\TestData\2073372.jpg", FileMode.Open);
            memoryStream = new MemoryStream();
            using (var br = new BinaryReader(fileData))
                memoryStream.Write(br.ReadBytes((int)fileData.Length), 0, (int)fileData.Length);
            service.PutFile(site.SiteSettings.PortalTag, @"t", "AnImage.jpg", "image/jpeg", memoryStream);

            fileData = new FileStream(@"..\..\TestData\2073372.jpg", FileMode.Open);
            memoryStream = new MemoryStream();
            using (var br = new BinaryReader(fileData))
                memoryStream.Write(br.ReadBytes((int)fileData.Length), 0, (int)fileData.Length);
            service.PutFile(site.SiteSettings.PortalTag, @"l", "AnImage.jpg", "image/jpeg", memoryStream);

            var exists = service.FileExists(site.SiteSettings.PortalTag + "/i/AnImage.jpg");
            Assert.IsTrue(exists);

            var list = new List<string>();
            list.Add("AnImage.jpg");
            var result = service.DeleteFiles(site.SiteSettings.PortalTag, list);
            Assert.That(result, Is.EqualTo(3));
            exists = service.FileExists(site.SiteSettings.PortalTag + "/i/AnImage.jpg");
            Assert.IsFalse(exists);

        }

        [Test]
        public async Task TryRemoveBucketNoFilesTest()
        {
            var site = new Site { SiteSettings = { PortalTag = "APortalTag" } };
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            var service = new AwsService();

            var result = service.TryCreateBucket("ABucket");

            var exists = service.TryRemoveBucket("ABucket");
            Assert.AreEqual(exists, 1);
        }

        [Test]
        public async Task TryRemoveBucketWithFilesTest()
        {
            var service = new AwsService();

            var result = service.TryCreateBucket("ABucket");

            var fileData = new FileStream(@"..\..\TestData\2073372.jpg", FileMode.Open);
            var memoryStream = new MemoryStream();
            var memoryStream2 = new MemoryStream();
            using (var br = new BinaryReader(fileData))
                memoryStream.Write(br.ReadBytes((int)fileData.Length), 0, (int)fileData.Length);

            fileData = new FileStream(@"..\..\TestData\2073372.jpg", FileMode.Open);
            using (var br = new BinaryReader(fileData))
                memoryStream2.Write(br.ReadBytes((int)fileData.Length), 0, (int)fileData.Length);

            service.PutFile("ABucket", @"i", "AnImage.jpg", "image/jpeg", memoryStream);
            service.PutFile("ABucket", @"t", "AnImage2.jpg", "image/jpeg", memoryStream2);

            var deletedCount = service.TryRemoveBucket("ABucket");
            Assert.AreEqual(deletedCount, 3);
        }

        

    }
}

