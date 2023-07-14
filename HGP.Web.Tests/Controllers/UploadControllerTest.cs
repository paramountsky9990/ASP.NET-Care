using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Routing;
using AspNet.Identity.MongoDB;
using HGP.Common.Database;
using HGP.Web.Controllers;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Assets;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Moq;
using MyPhotos.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace HGP.Web.Tests.Controllers
{
    [TestFixture]
    public class UploadControllerTest
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
        public void UploadControllerConstructorTest()
        {
            var controller = IoC.Container.GetInstance<UploadController>();
            Assert.IsNotNull(controller);
        }

        [Test]
        public void AttachOneImageFileTest()
        {
            
            var site = new Site() { Id = "ASiteId", SiteSettings = { PortalTag = "APortalTag" } };
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            var asset = new Asset() { PortalId = site.Id, HitNumber = "2073372" };
            var assetService = IoC.Container.GetInstance<IAssetService>();
            assetService.Save(asset);
       
            var fileData = new FileStream(@"..\..\TestData\2073372.jpg", FileMode.Open);
            var memoryStream = new MemoryStream();
            var memoryStream2 = new MemoryStream();
            using (var br = new BinaryReader(fileData))
                memoryStream.Write(br.ReadBytes((int)fileData.Length), 0, (int)fileData.Length);

            var controller = IoC.Container.GetInstance<UploadController>();
            var privateController = new PrivateObject(controller);
            var result = privateController.Invoke("AttachFile", "2073372.jpg", memoryStream, null);

            Assert.That(result, Is.EqualTo(1), "Bad result from AttachFile");

            var targetAsset = assetService.GetById(asset.Id);
            Assert.That(targetAsset.Media, Is.Not.Null);
            Assert.That(targetAsset.Media.Count, Is.EqualTo(1));
            Assert.That(targetAsset.Media.First().FileName, Is.EqualTo("2073372.jpg"));
        }

        [Test]
        public void ProcessZipFileWithOneImageTest()
        {
            var site = new Site() { Id = "ASiteId", SiteSettings = { PortalTag = "APortalTag" } };
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            var asset = new Asset() { PortalId = site.Id, HitNumber = "2073372" };
            var assetService = IoC.Container.GetInstance<IAssetService>();
            assetService.Save(asset);

            var fileData = new FileStream(@"..\..\TestData\2073372.zip", FileMode.Open);
            var memoryStream = new MemoryStream();
            var memoryStream2 = new MemoryStream();
            using (var br = new BinaryReader(fileData))
                memoryStream.Write(br.ReadBytes((int)fileData.Length), 0, (int)fileData.Length);
            memoryStream.Flush();
            memoryStream.Position = 0;

            var postedFileData = new MyTestPostedFileBase(memoryStream, "application/zip", "2073372.zip");

            var controller = IoC.Container.GetInstance<UploadController>();
            var privateController = new PrivateObject(controller);
            var result = privateController.Invoke("ProcessZipFile", postedFileData);

            Assert.That(result, Is.EqualTo(1), "Bad result from ProcessZipFile");

            var targetAsset = assetService.GetById(asset.Id);
            Assert.That(targetAsset.Media, Is.Not.Null);
            Assert.That(targetAsset.Media.Count, Is.EqualTo(1));
            Assert.That(targetAsset.Media.First().FileName, Is.EqualTo("2073372.JPG"));
        }

        [Test]
        public void ProcessZipFileWithThreeImagesTest()
        {
            var site = new Site() { Id = "ASiteId", SiteSettings = { PortalTag = "APortalTag" } };
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            var asset = new Asset() { PortalId = site.Id, HitNumber = "2073372" };
            var assetService = IoC.Container.GetInstance<IAssetService>();
            assetService.Save(asset);

            var fileData = new FileStream(@"..\..\TestData\3Images2073372.zip", FileMode.Open);
            var memoryStream = new MemoryStream();
            var memoryStream2 = new MemoryStream();
            using (var br = new BinaryReader(fileData))
                memoryStream.Write(br.ReadBytes((int)fileData.Length), 0, (int)fileData.Length);
            memoryStream.Flush();
            memoryStream.Position = 0;

            var postedFileData = new MyTestPostedFileBase(memoryStream, "application/zip", "3Images2073372.zip");

            var controller = IoC.Container.GetInstance<UploadController>();
            var privateController = new PrivateObject(controller);
            var result = privateController.Invoke("ProcessZipFile", postedFileData);

            Assert.That(result, Is.EqualTo(3), "Bad result from ProcessZipFile");

            var targetAsset = assetService.GetById(asset.Id);
            Assert.That(targetAsset.Media, Is.Not.Null);
            Assert.That(targetAsset.Media.Count, Is.EqualTo(3));
            Assert.That(targetAsset.Media.First(x => x.SortOrder == 1).FileName, Is.EqualTo("2073372.JPG"));
            Assert.That(targetAsset.Media.First(x => x.SortOrder == 2).FileName, Is.EqualTo("2073372-2.JPG"));
            Assert.That(targetAsset.Media.First(x => x.SortOrder == 3).FileName, Is.EqualTo("2073372-3.JPG"));
        }
        [Test]
        public void ImportExcelDataTest()
        {
            var siteService = IoC.Container.GetInstance<ISiteService>();
            var site = new Site() { Id = "ASiteId" };
            site.Locations.Add(new Location() { Name = "Room 1" });
            site.Locations.Add(new Location() { Name = "Room 2" });
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            var user = new PortalUser();
            IoC.Container.GetInstance<IWorkContext>().CurrentUser = user;

            var controller = IoC.Container.GetInstance<UploadController>();
            var model = new ReviewAssetUploadModel()
            {
                FullFileName = @"../../TestData/12 - Asset Upload Minimum Columns.xlsx",
                SiteId = site.Id
            };

            var privateController = new PrivateObject(controller);
            privateController.Invoke("ImportExcelData", model, true);

            Assert.That(model.ImportResult, Is.EqualTo(12));

            var assetService = IoC.Container.GetInstance<IAssetService>();
            var asset = assetService.Repository.GetAll<Asset>().First();

            Assert.That(asset.HitNumber, Is.EqualTo("2073357"));
            Assert.That(asset.BookValue, Is.EqualTo("100.01"));
        }


        [Test]
        public void ImportExcelCustomColumnTest()
        {
            var siteService = IoC.Container.GetInstance<ISiteService>();
            var site = new Site() { Id = "ASiteId" };
            site.Locations.Add(new Location() { Name = "Room 1" });
            site.Locations.Add(new Location() { Name = "Room 2" });
            siteService.Save(site);
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            var user = new PortalUser();
            IoC.Container.GetInstance<IWorkContext>().CurrentUser = user;

            var controller = IoC.Container.GetInstance<UploadController>();
            var model = new ReviewAssetUploadModel()
            {
                FullFileName = @"../../TestData/Asset Upload Custom Column.xlsx",
                SiteId = site.Id
            };

            var privateController = new PrivateObject(controller);
            privateController.Invoke("ImportExcelData", model, true);

            Assert.That(model.ImportResult, Is.EqualTo(12));

            var assetService = IoC.Container.GetInstance<IAssetService>();
            var asset = assetService.Repository.GetAll<Asset>().FirstOrDefault(x => x.HitNumber == "2073359");

            var targetJson = JsonConvert.DeserializeObject(asset.CustomData) as JArray;
            Assert.That(targetJson, Is.Not.Null);
            Assert.That(targetJson.ToString(Formatting.None), Is.EqualTo("[{\"key\":\"A Custom Column\",\"value\":\"1\"}]"));
        }

        //[Test]
        //public void FakeUploadFiles()
        //{
        //    //We'll need mocks (fake) of Context, Request and a fake PostedFile
        //    var request = new Mock<HttpRequestBase>();
        //    var context = new Mock<HttpContextBase>();
        //    var postedfile = new Mock<HttpPostedFileBase>();

        //    //Someone is going to ask for Request.File and we'll need a mock (fake) of that.
        //    var postedfilesKeyCollection = new Mock<HttpFileCollectionBase>();
        //    var fakeFileKeys = new List<string>() { "file" };

        //    //OK, Mock Framework! Expect if someone asks for .Request, you should return the Mock!
        //    context.Expect(ctx => ctx.Request).Returns(request.Object);
        //    //OK, Mock Framework! Expect if someone asks for .Files, you should return the Mock with fake keys!
        //    request.Expect(req => req.Files).Returns(postedfilesKeyCollection.Object);

        //    //OK, Mock Framework! Expect if someone starts foreach'ing their way over .Files, give them the fake strings instead!
        //    postedfilesKeyCollection.Expect(keys => keys.GetEnumerator()).Returns(fakeFileKeys.GetEnumerator());

        //    //OK, Mock Framework! Expect if someone asks for file you give them the fake!
        //    postedfilesKeyCollection.Expect(keys => keys["file"]).Returns(postedfile.Object);

        //    //OK, Mock Framework! Give back these values when asked, and I will want to Verify that these things happened
        //    postedfile.Expect(f => f.ContentLength).Returns(8192).Verifiable();
        //    postedfile.Expect(f => f.FileName).Returns("foo.doc").Verifiable();

        //    //OK, Mock Framework! Someone is going to call SaveAs, but only once!
        //    postedfile.Expect(f => f.SaveAs(It.IsAny<string>())).AtMostOnce().Verifiable();

        //    HomeController controller = new HomeController(IoC.Container.GetInstance<IPortalServices>());
        //    //Set the controller's context to the mock! (fake)
        //    controller.ControllerContext = new ControllerContext(context.Object, new RouteData(), controller);

        //    //DO IT!
        //    //ViewResult result = controller.UploadFiles() as ViewResult;

        //    ////Now, go make sure that the Controller did its job
        //    //var uploadedResult = result.ViewData.Model as List<ViewDataUploadFilesResult>;
        //    //Assert.AreEqual(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "foo.doc"), uploadedResult[0].Name);
        //    //Assert.AreEqual(8192, uploadedResult[0].Length);

        //    postedfile.Verify();
        //}


    }

    internal class MyTestPostedFileBase : HttpPostedFileBase
    {
        private Stream stream;
        private string contentType;
        private string fileName;

        public MyTestPostedFileBase(Stream stream, string contentType, string fileName)
        {
            this.stream = stream;
            this.contentType = contentType;
            this.fileName = fileName;
        }

        public override int ContentLength
        {
            get { return (int) stream.Length; }
        }

        public override string ContentType
        {
            get { return contentType; }
        }

        public override string FileName
        {
            get { return fileName; }
        }

        public override Stream InputStream
        {
            get { return stream; }
        }

        public override void SaveAs(string filename)
        {
            throw new NotImplementedException();
        }
    }
}

