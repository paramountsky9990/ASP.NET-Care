using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Hosting;
using System.Web.Script.Serialization;
using AspNet.Identity.MongoDB;
using AutoMapper;
using HGP.Common;
using HGP.Common.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Email;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Moq;

namespace HGP.Web.Tests.Services
{
    [TestFixture]
    public class EmailServiceTests
    {

        #region Additional test attributes
        public SiteSettings SiteSettings { get; set; }
        public IWorkContext WorkContext { get; set; }
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        [OneTimeSetUp]
        public static void MyClassInitialize()
        {
            RegisterDependencies.InitStructureMap();
            RegisterDependencies.InitMongo();

            Mapper.Initialize(cfg =>
                {
                    cfg.AddProfiles(typeof(HGP.Web.MvcApplication).Assembly);
                    cfg.IgnoreUnmapped();
                    cfg.CreateMap<DraftAsset, EmailTaskModel>();
                    cfg.CreateMap<AssetRequestDetail, AssetRequestEmailDto>();
                    cfg.CreateMap<Request, EmailTaskModel>();
                    cfg.CreateMap<Request, AssetApprovedEmailModel>();
                    cfg.CreateMap<Request, AssetDeniedEmailModel>();
                    cfg.CreateMap<DraftAsset, DraftAssetDeniedEmailModel>();
                }
            );
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
            this.SiteSettings = new SiteSettings() {PortalTag = "testtag"};
            this.WorkContext = new TestWorkContext() { PortalTag = this.SiteSettings.PortalTag };
            this.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));

            var repository = IoC.Container.GetInstance<IMongoRepository>();
            repository.AllowDatabaseDrop = true;
            repository.DropDatabase();

            var client = new MongoClient(WebConfigurationManager.AppSettings["MongoDbConnectionString"]);
            var dbName = WebConfigurationManager.AppSettings["MongoDbName"];

            //todo: Move to config file
            var database = client.GetServer().GetDatabase(dbName);

            var users = database.GetCollection<IdentityUser>("PortalUsers");
            var roles = database.GetCollection<IdentityRole>("Roles");
            this.UserStore = new UserStore<PortalUser>(new ApplicationIdentityContext(users, roles));
            var userManager = new PortalUserService(this.UserStore);
            IoC.Container.Configure(x => x.For<PortalUserService>().Use(userManager));

            var path = AppDomain.CurrentDomain.BaseDirectory;
            var data = File.ReadAllText(path + @"/TestData/EmailTemplates/TestEmail.html");
            var template = new EmailTemplate()
            {
                PortalId = "", // Base templates do not get a portalId
                TemplateType = GlobalConstants.EmailTypes.EmailTest.ToString(),
                Data = data
            };
            IoC.Container.GetInstance<IEmailService>().Save(template);
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
            var service = new EmailService();
            Assert.IsNotNull(service);
        }

        [Test]
        public void SendEmailTest()
        {
            var model = new EmailTestModel(this.WorkContext.HttpContext, this.SiteSettings);

            model.Subject = "Test message sent at " + DateTime.Now.ToShortTimeString();

            model.AssetModel = new AssetRequestEmailDto() { Title = "Test title" };


            try
            {
                var sender = new EmailSender<EmailTestModel>();
                var resultTask = sender.SendEmail(GlobalConstants.EmailTypes.EmailTest, model);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [Test]
        public void SendAssetEmailTest()
        {
            var model = new EmailTestModel(this.WorkContext.HttpContext, this.SiteSettings);

            model.Subject = "Test message sent at " + DateTime.Now.ToShortTimeString();

            model.AssetModel = new AssetRequestEmailDto() { Title = "Test title" };

            var requestDetail = new AssetRequestDetail();
            requestDetail.CustomData = "[{\"key\":\"key1\",\"value\":\"value1\"},{\"key\":\"key2\",\"value\":\"value2\"}]";

            var requestDetailDto = Mapper.Map<AssetRequestDetail, AssetRequestEmailDto>(requestDetail);

            var jsonObject = JsonConvert.DeserializeObject<List<KeyValuePair<string, string>>>(requestDetailDto.CustomData);

            requestDetailDto.CustomDataPairs = jsonObject;

            model.AssetModel = requestDetailDto;

            try
            {
                var sender = new EmailSender<EmailTestModel>();
                var resultTask = sender.SendEmail(GlobalConstants.EmailTypes.EmailTest, model);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        [Test]
        public void TestDoSendAssetApprovedNotification()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);
            var request = GetTestRequest(site, requestor);

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));

            try
            {
                var resultTask = service.DoSendAssetApprovedNotification(site, requestor, request, request.AssetRequests[0]);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [Test]
        public async Task TestSendLocationPendingApprovalNotification()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);
            var request = GetTestRequest(site, requestor);
            var userManager = new PortalUserService(this.UserStore);
            var result = await userManager.CreateAsync(requestor, "123456");
            var assetManager = userManager.FindByEmail("rrb@matrix6.com");
            request.ApprovingManagerId = assetManager.Id;

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null,null, new SiteService(), service.WorkContext);

            var siteService = new SiteService();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            try
            {
                var resultTask = service.SendLocationPendingApprovalNotification(site, requestor, request, request.AssetRequests[0]);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [Test]
        public async Task TestSendManagerPendingApproval()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);
            var request = GetTestRequest(site, requestor);
            var userManager = new PortalUserService(this.UserStore);
            var result = await userManager.CreateAsync(requestor, "123456");
            var assetManager = userManager.FindByEmail("rrb@matrix6.com");
            request.ApprovingManagerId = assetManager.Id;

            var approver1 = GetTestApprover(site, "postmaster@matrix6.com");
            var approver2 = GetTestApprover(site, "rboarman@gmail.com");
            result = await userManager.CreateAsync(approver1, "123456");
            result = await userManager.CreateAsync(approver2, "123456");

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null,null, new SiteService(), service.WorkContext);

            var siteService = new SiteService();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            try
            {
                var resultTask = service.SendManagerPendingApproval(site, requestor, request, request.AssetRequests[0]);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }


        [Test]
        public async Task TestDoSendRequestApprovedToOthers()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);
            var request = GetTestRequest(site, requestor);
            var userManager = new PortalUserService(this.UserStore);
            var result = await userManager.CreateAsync(requestor, "123456");
            var assetManager = userManager.FindByEmail("rrb@matrix6.com");
            request.ApprovingManagerId = assetManager.Id;

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null,null, new SiteService(), service.WorkContext);

            var siteService = new SiteService();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            site.Locations.First().OwnerId = requestor.Id;

            site.SiteSettings.ApprovalCcAddresses.Add("rrb@matrix6.com");
            site.SiteSettings.ApprovalCcAddresses.Add("rboareman@gmail.com");
            siteService.Save(site);

            try
            {
                var resultTask = service.DoSendRequestApprovedToOthers(site, requestor, request, request.AssetRequests[0]);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [Test]
        public async Task TestDoSendRequestDeniedNotification()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);
            var request = GetTestRequest(site, requestor);
            var userManager = new PortalUserService(this.UserStore);
            var result = await userManager.CreateAsync(requestor, "123456");
            var assetManager = userManager.FindByEmail("rrb@matrix6.com");
            request.ApprovingManagerId = assetManager.Id;

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null, null, new SiteService(), service.WorkContext);

            var siteService = new SiteService();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            try
            {
                var resultTask = service.DoSendRequestDeniedNotification(site, requestor, request, request.AssetRequests[0]);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        [Test]
        public async Task TestSendResetPasswordNotification()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null, null, new SiteService(), service.WorkContext);

            try
            {
                var resultTask = service.SendResetPasswordNotification(requestor, site, "callbackurl");
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [Test]
        public async Task TestSendWelcomeMessage()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null, null, new SiteService(), service.WorkContext);

            try
            {
                var resultTask = service.SendWelcomeMessage(requestor, site, "callbackurl");
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [Test]
        public async Task TestSendWelcomeMessage4AdminUser()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));

            try
            {
                var resultTask = service.SendWelcomeMessage4AdminUser(requestor, site);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [Test]
        public void TestSendAssetUploadSummary()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);

            var assets = new List<AssetsUploaded>();
            var assetsUploaded = new AssetsUploaded()
            {
                Asset = GetTestAsset(),
                PrimaryImageURL = "https://s3-us-west-1.amazonaws.com/hgpmedia/test1/t/2073357.JPG",
                AssetURL = @"https://thisistheasseturl/blahdeblah"
            };
            assets.Add(assetsUploaded);
            assets.Add(assetsUploaded);
            assets.Add(assetsUploaded);

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));

            try
            {
                var resultTask = service.SendAssetUploadSummary(service.WorkContext.HttpContext, site, requestor, assets);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [Test]
        public void TestSendLocationNotificationApproved()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);
            var request = GetTestRequest(site, requestor);

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));

            try
            {
                var resultTask = service.SendLocationNotificationApproved(site, requestor, request, request.AssetRequests[0]);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [Test]
        public async Task TestSendOwnerNotificationPendingApproval()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);
            var request = GetTestRequest(site, requestor);
            var userManager = new PortalUserService(this.UserStore);
            var result = await userManager.CreateAsync(requestor, "123456");
            var assetManager = userManager.FindByEmail("rrb@matrix6.com");
            request.ApprovingManagerId = assetManager.Id;

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null, null, new SiteService(), service.WorkContext);

            var siteService = new SiteService();
            IoC.Container.GetInstance<IWorkContext>().CurrentSite = site;
            site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
            siteService.Save(site);

            site.Locations.First().OwnerId = requestor.Id;
            siteService.Save(site);

            try
            {
                var resultTask = service.SendOwnerNotificationPendingApproval(site, requestor, request, request.AssetRequests[0]);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex)
            {

                throw;
            }
        }


        [Test]
        public void TestSendPendingRequestReminder()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);
            var request = GetTestRequest(site, requestor);

            List<PendingRequestAssets> PendingRequestAssets = new List<PendingRequestAssets>();

            PendingRequestAssets asset = new PendingRequestAssets()
            {
                Asset = GetTestAsset(),
                PrimaryImageURL = "https://s3-us-west-1.amazonaws.com/hgpmedia/test1/t/2073357.JPG"
            };

            PendingRequestAssets.Add(asset);
            PendingRequestAssets.Add(asset);

            PendingRequestReminderModel pendingReqReminderModel = new PendingRequestReminderModel(this.WorkContext.HttpContext, site.SiteSettings);
            pendingReqReminderModel.Request = request;
            pendingReqReminderModel.DaysReqWaited = 3;
            pendingReqReminderModel.PortalTag = "test1";
            pendingReqReminderModel.InboxURL = @"http://thisistheinboxurl";
            pendingReqReminderModel.PendingRequestAssets = PendingRequestAssets;

            var service = IoC.Container.GetInstance<IEmailService>();
            try
            {
                var resultTask = service.SendPendingRequestReminder(pendingReqReminderModel);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex) { throw; }
        }

        [Test]
        public void TestSendRequestAssetNotAvailable()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);
            var request = GetTestRequest(site, requestor);
            var asset = GetTestAsset();

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null, null, new SiteService(), service.WorkContext);

            try
            {
                var resultTask = service.SendRequestAssetNotAvailable(site, request, asset);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex) { throw; }
        }

        [Test]
        public void TestSendWishListMatchedAssets()
        {
            var site = GetTestSite();
            var requestor = GetTestRequestor(site);

            var assets = new List<AssetsUploaded>();
            var assetsUploaded = new AssetsUploaded()
            {
                Asset = GetTestAsset(),
                PrimaryImageURL = @"https://s3-us-west-1.amazonaws.com/hgpmedia/test1/t/2073357.JPG",
                AssetURL = @"https://thisistheasseturl/blahdeblah"
            };
            assets.Add(assetsUploaded);
            assets.Add(assetsUploaded);
            assets.Add(assetsUploaded);

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null, null, new SiteService(), service.WorkContext);


            WishList wishList = new WishList
            {
                Id = ObjectId.GenerateNewId().ToString(),
                PortalId = ObjectId.GenerateNewId().ToString(),
                PortalUserId = ObjectId.GenerateNewId().ToString(),
                SearchCriteria = "WishListSearchCriteria"
            };

            try
            {

                var resultTask = service.SendWishListMatchedAssets(requestor, assets, wishList, service.WorkContext.HttpContext, site);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex) { throw; }
        }

        [Test]
        public async Task SendDraftAssetPendingApproval()
        {
            var site = GetTestSite();
            var siteService = IoC.Container.GetInstance<ISiteService>();
            siteService.Save(site);

            var approver1 = GetTestApprover(site, "postmaster@matrix6.com");
            var approver2 = GetTestApprover(site, "rboarman@gmail.com");
            var userManager = new PortalUserService(this.UserStore);
            var result = await userManager.CreateAsync(approver1, "123456");
            result = await userManager.CreateAsync(approver2, "123456");
            var asset = new DraftAsset()
            {
                BookValue = "1000",
                Category = "Category",
                ClientIdNumber = "ClientIdNumber",
                Description = "This is a Description",
                HitNumber = "HitNumber",
                Location = "Location",
                Manufacturer = "Manufacturer",
                ModelNumber = "ModelNumber",
                SerialNumber = "SerialNumber",
                OwnerFirstName = "OwnerFirstName",
                OwnerLastName = "OwnerLastName",
                OwnerEmail = "rrb@matrix6.com",
                OwnerPhone = "OwnerPhone",
                Title = "Title",
                Media =
                    new List<MediaFileDto>()
                    {
                        new MediaFileDto()
                        {
                            ContentType = "image/jpeg",
                            FileName = "2073357.JPG",
                            IsImage = true,
                            SortOrder = 1
                        }
                    },
            };
            
            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null, null, new SiteService(), service.WorkContext);

            try
            {

                var resultTask = service.SendDraftAssetPendingApproval(site, asset, service.WorkContext.HttpContext);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex) { throw; }
        }

        [Test]
        public async Task SendDraftAssetDeniedApproval()
        {
            var site = GetTestSite();
            var siteService = IoC.Container.GetInstance<ISiteService>();
            siteService.Save(site);

            var approver1 = GetTestApprover(site, "postmaster@matrix6.com");
            var approver2 = GetTestApprover(site, "rboarman@gmail.com");
            var userManager = new PortalUserService(this.UserStore);
            var result = await userManager.CreateAsync(approver1, "123456");
            result = await userManager.CreateAsync(approver2, "123456");
            var asset = new DraftAsset()
            {
                BookValue = "1000",
                Category = "Category",
                ClientIdNumber = "ClientIdNumber",
                Description = "This is a Description",
                HitNumber = "HitNumber",
                Location = "Location",
                Manufacturer = "Manufacturer",
                ModelNumber = "ModelNumber",
                SerialNumber = "SerialNumber",
                OwnerFirstName = "OwnerFirstName",
                OwnerLastName = "OwnerLastName",
                OwnerEmail = "rrb@matrix6.com",
                OwnerPhone = "OwnerPhone",
                OwnerId = "OwnerId",
                Title = "Title",
                Notes = "This is a deny mnessage",
                Media =
                    new List<MediaFileDto>()
                    {
                        new MediaFileDto()
                        {
                            ContentType = "image/jpeg",
                            FileName = "2073357.JPG",
                            IsImage = true,
                            SortOrder = 1
                        }
                    },
            };

            var service = IoC.Container.GetInstance<IEmailService>();
            service.WorkContext = new TestWorkContext() { PortalTag = "test1" };
            service.WorkContext.HttpContext = new HttpContext(new SimpleWorkerRequest("", "", "", "", new BufferedStringTextWriter(TextWriter.Null)));
            service.WorkContext.S = new PortalServices(null, null, null, null, null, null, null, null, null, null, null, null, null, new SiteService(), service.WorkContext);

            try
            {
                var resultTask = service.SendDraftAssetDeniedApproval(site, asset, "Asset denied message", service.WorkContext.HttpContext);
                Assert.That(resultTask.Result.SendStatus, Is.EqualTo(SendMessageStatus.Success));
            }
            catch (Exception ex) { throw; }
        }


        private Request GetTestRequest(Site site, PortalUser user)
        {
            var request = new Request()
            {
                ApprovingManagerName = user.ApprovingManagerName,
                ApprovingManagerEmail = user.ApprovingManagerEmail,
                ApprovingManagerPhone = user.ApprovingManagerPhone,
                Id = ObjectId.GenerateNewId().ToString(),
                AssetCount = 1,
                ClosedDate = DateTime.Now,
                RequestDate = DateTime.Now,
                RequestNum = "RequestNum",
                RequestorEmail = "rrb@matrix6.com",
                RequestorName = "Requestor Name",
                RequestorPhone = "RequestorPhone",
                ShipToAddress = GetTestAddress(),
                Status = GlobalConstants.RequestStatusTypes.Approved,
                Notes = "This is a request note. This is a note. This is a note. This is a note. This is a note. This is a note. ",
                AssetRequests = GetTestAssetRequestDetail()

            };

            var assetRequest = GetTestAssetRequestDetail();


            return request;
        }

        private List<AssetRequestDetail> GetTestAssetRequestDetail()
        {
            var assetRequestDetail = new AssetRequestDetail()
            {
                AssetAddress = GetTestAddress(),
                BookValue = "100",
                Catalog = "Catalog",
                Category = "Category",
                ClientIdNumber = "ClientIdNumber",
                CustomData = "[{\"key\":\"key1\",\"value\":\"value1\"},{\"key\":\"key2\",\"value\":\"value2\"}]",
                DisplayBookValue = true,
                HitNumber = "HitNumber",
                Id = ObjectId.GenerateNewId().ToString(),
                LocationName = "LocationName",
                LocationOwnerEmail = "rrb@matrix6.com",
                LocationOwnerName = "LocationOwnerName",
                LocationOwnerPhone = "LocationOwnerPhone",
                OwnerEmail = "rrb@matrix6.com",
                Manufacturer = "Manufacturer",
                Media =
                    new List<AdminAssetsHomeGridMediaModel>()
                    {
                        new AdminAssetsHomeGridMediaModel()
                        {
                            ContentType = "image/jpeg",
                            FileName = "2073357.JPG",
                            IsImage = true,
                            SortOrder = 1
                        }
                    },
                ModelNumber = "ModelNumber",
                OwnerName = "Owner Name",
                OwnerPhone = "OwnerPhone",
                Quantity = 1,
                SerialNumber = "SerialNumber",
                Status = GlobalConstants.RequestStatusTypes.Approved,
                TaskComment = "TaskComment",
                Title = "Title Title Title",
            };

            return new List<AssetRequestDetail>() { assetRequestDetail };
        }

        private Asset GetTestAsset()
        {
            var assetRequestDetail = new Asset()
            {
                BookValue = "100",
                Catalog = "Catalog",
                Category = "Category",
                ClientIdNumber = "ClientIdNumber",
                CustomData = "[{\"key\":\"key1\",\"value\":\"value1\"},{\"key\":\"key2\",\"value\":\"value2\"}]",
                DisplayBookValue = true,
                HitNumber = "HitNumber",
                Id = ObjectId.GenerateNewId().ToString(),
                Location = "LocationName",
                Manufacturer = "Manufacturer",
                Media =
                    new List<MediaFileDto>()
                    {
                        new MediaFileDto()
                        {
                            ContentType = "image/jpeg",
                            FileName = "2073357.JPG",
                            IsImage = true,
                            SortOrder = 1
                        }
                    },
                ModelNumber = "ModelNumber",
                Quantity = 1,
                SerialNumber = "SerialNumber",
                Status = GlobalConstants.AssetStatusTypes.Available,
                Title = "Title Title Title",
            };

            return assetRequestDetail;
        }

        private Address GetTestAddress()
        {
            var address = new Address()
            {
                Attention = "Attention",
                City = "City",
                Country = "Country",
                Notes = "This is an address note. This is a note. This is a note. This is a note. This is a note. This is a note. ",
                State = "State",
                Street1 = "Street1",
                Street2 = "Street2",
                Zip = "Zip"
            };

            return address;

        }

        private PortalUser GetTestRequestor(Site site)
        {
            var user = new PortalUser()
            {
                Address = GetTestAddress(),
                ApprovingManagerEmail = "rrb@matrix6.com",
                ApprovingManagerName = "Approving Manager Name",
                ApprovingManagerPhone = "Approving Manager Phone",
                FirstName = "FirstName",
                LastName = "LastName",
                LastLogin = DateTime.Now,
                PortalId = site.Id,
                Email = "rrb@matrix6.com",
                PhoneNumber = "PhoneNumber",
                UserName = "UserName"
            };


            return user;
        }

        private PortalUser GetTestApprover(Site site, string email)
        {
            var user = new PortalUser()
            {
                Address = GetTestAddress(),
                ApprovingManagerEmail = "",
                ApprovingManagerName = "",
                ApprovingManagerPhone = "",
                FirstName = "ApproverFirstName",
                LastName = "ApproverLastName",
                LastLogin = DateTime.Now,
                PortalId = site.Id,
                Email = email,
                PhoneNumber = "ApproverPhoneNumber",
                UserName = email,
            };

            user.Roles.Add("Approver");
            user.Roles.Add("ClientAdmin");

            return user;
        }

        private Site GetTestSite()
        {
            var site = new Site();
            site.Id = ObjectId.GenerateNewId().ToString();
            site.SiteSettings.PortalTag = "test1";

            return site;
        }

        [Test]
        public void SaveTest()
        {
            var template = new EmailTemplate() {TemplateType = "ATemplateType", Data = "SomeData"};
            var service = IoC.Container.GetInstance<IEmailService>();
            service.Save(template);

            var target = service.Repository.GetAll<EmailTemplate>().FirstOrDefault(x => x.TemplateType == "ATemplateType");
            Assert.That(target, Is.Not.Null);
            Assert.That(target.Data, Is.EqualTo("SomeData"));
        }
    }
}

