using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Configuration;
using AspNet.Identity.MongoDB;
using HGP.Common;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Email;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace HGP.Web.Database
{
    public class InitDatabase
    {
        public static ILogger Logger { get; set; }

        public static async void GenData()
        {
            Logger = Log4NetLogger.GetLogger();

            var s = IoC.Container.GetInstance<IWorkContext>().S;

            try
            {
                var client = new MongoClient(WebConfigurationManager.AppSettings["MongoDbConnectionString"]);
                var database = client.GetServer().GetDatabase(WebConfigurationManager.AppSettings["MongoDbName"]);
                var requests = database.GetCollection<Request>("Requests");
                //todo: Research index too large error 
                //requests.CreateIndex(IndexKeys<Request>.Ascending(_ => _.AssetRequests));
            }
            catch (Exception ex)
            {
                
                throw;
            }
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(ApplicationIdentityContext.Create()));
            if (!roleManager.RoleExists("SuperAdmin"))
                roleManager.Create(new IdentityRole("SuperAdmin"));
            if (!roleManager.RoleExists("CareAdmin"))
                roleManager.Create(new IdentityRole("CareAdmin"));
            if (!roleManager.RoleExists("ClientAdmin"))
                roleManager.Create(new IdentityRole("ClientAdmin"));
            if (!roleManager.RoleExists("Requestor"))
                roleManager.Create(new IdentityRole("Requestor"));
            if (!roleManager.RoleExists("Approver"))
                roleManager.Create(new IdentityRole("Approver"));

            Site adminSite = null;
            if (!s.SiteService.Repository.All<Site>().Any())
            {
                adminSite = new Site {SiteSettings = {IsAdminPortal = true, PortalTag = "admin"}};
                s.SiteService.Save(adminSite);
                var result = await TryAddUser(adminSite, "rrb@matrix6.com", "rrb@matrix6.com", "gamma12", "SuperAdmin", "Rick", "Boarman", "6505551212", new Address() { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043" });
                result = await TryAddUser(adminSite, "gdove@hginc.com", "gdove@hginc.com", "Welcome1!", "SuperAdmin", "Grayson", "Dove", "8588470655", new Address() { Street1 = "12625 High Bluff Drive", City = "San Diego", State = "CA", Zip = "92130" });
                result = await TryAddUser(adminSite, "tlaster@hginc.com", "tlaster@hginc.com", "Welcome1!", "SuperAdmin", "Tom", "Laster", "8588470655", new Address() { Street1 = "12625 High Bluff Drive", City = "San Diego", State = "CA", Zip = "92130" });
                result = await TryAddUser(adminSite, "ndove@hginc.com", "ndove@hginc.com", "Welcome1!", "SuperAdmin", "Nick", "Dove", "8588470655", new Address() { Street1 = "12625 High Bluff Drive", City = "San Diego", State = "CA", Zip = "92130" });
                result = await TryAddUser(adminSite, "kdove@kdove.com", "kdove@kdove.com", "Welcome1!", "SuperAdmin", "Kirk", "Dove", "8588470655", new Address() { Street1 = "12625 High Bluff Drive", City = "San Diego", State = "CA", Zip = "92130" });

                var site = new Site { SiteSettings = { IsAdminPortal = false, PortalTag = "acmeco", CompanyName = "Acme Co.", Phone = "6195551212", Street1 = "345 Any St.", City = "San Diego", State = "CA", Zip = "92093", SupportEmail = "help@care.com"} };
                site.Locations.Add(new Location() { Name = "First Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
                site.Locations.Add(new Location() { Name = "Second Floor", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
                site.Locations.Add(new Location() { Name = "Storage Room 1", Address = { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043", Country = "USA" } });
                s.SiteService.Save(site);

                var result2 = TryAddUser(site, "ca", "clientadmin1@matrix6.com", "gamma12", "ClientAdmin", "Jane", "ClientAdmin", "6505551212", new Address() { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043" });
                var result3 = TryAddUser(site, "r", "requestor1@matrix6.com", "gamma12", "Requestor", "Joe", "Requestor", "6505551212", new Address() { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043" });
                var result4 = TryAddUser(site, "o", "owner1@matrix6.com", "gamma12", "Requestor", "Owen", "Owner", "6505551212", new Address() { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043" });
                var result5 = TryAddUser(site, "m", "manager1@matrix6.com", "gamma12", "Requestor", "Manny", "Manager", "6505551212", new Address() { Street1 = "123 Easy St.", City = "Mountain View", State = "CA", Zip = "94043" });
                var ownerId = result4.Result.Id;

                foreach (var location in site.Locations)
                {
                    location.OwnerId = ownerId;
                    location.OwnerName = result4.Result.FirstName + " " + result4.Result.LastName;
                    location.OwnerEmail = result4.Result.Email;
                    location.OwnerPhone = result4.Result.PhoneNumber;
                }
                s.SiteService.Save(site);
            }

            var parseEmails = bool.Parse(WebConfigurationManager.AppSettings["ParseEmailsOnLaunch"]);
            if (parseEmails || (!s.EmailService.Repository.All<EmailTemplate>().Any()))
            {
                SaveTemplate("Header.html", GlobalConstants.EmailTypes.Header);
                SaveTemplate("Footer.html", GlobalConstants.EmailTypes.Footer);
                SaveTemplate("AssetApprovedNotification.html", GlobalConstants.EmailTypes.AssetApprovedNotification);
                SaveTemplate("AssetUploadSummary.html", GlobalConstants.EmailTypes.AssetUploadSummary);
                SaveTemplate("LocationNotification.html", GlobalConstants.EmailTypes.LocationNotification);
                SaveTemplate("LocationPendingApproval.html", GlobalConstants.EmailTypes.LocationPendingApproval);
                SaveTemplate("ManagerNotification.html", GlobalConstants.EmailTypes.ManagerNotification);
                SaveTemplate("OwnerNotification.html", GlobalConstants.EmailTypes.OwnerNotification);
                SaveTemplate("RequestApprovedToOthers.html", GlobalConstants.EmailTypes.RequestApprovedToOthers);
                SaveTemplate("RequestDeniedNotification.html", GlobalConstants.EmailTypes.RequestDeniedNotification);
                SaveTemplate("ResetPasswordNotification.html", GlobalConstants.EmailTypes.ResetPasswordNotification);
                SaveTemplate("WelcomeNotification.html", GlobalConstants.EmailTypes.WelcomeNotification);
                SaveTemplate("WelcomeNotification4AdminUser.html", GlobalConstants.EmailTypes.WelcomeNotification4AdminUser);
                SaveTemplate("PendingRequestReminder.html", GlobalConstants.EmailTypes.PendingRequestReminder);
                SaveTemplate("RequestAssetNotAvailable.html", GlobalConstants.EmailTypes.RequestAssetNotAvailable);
                SaveTemplate("WishListMatchedAssets.html", GlobalConstants.EmailTypes.WishListMatchedAssets);
                SaveTemplate("ExpiringWishList.html", GlobalConstants.EmailTypes.ExpiringWishList);
                SaveTemplate("ExpiringAssets.html", GlobalConstants.EmailTypes.ExpiringAssets);
                SaveTemplate("DraftAssetPendingApproval.html", GlobalConstants.EmailTypes.DraftAssetPendingApproval);
                SaveTemplate("DraftAssetDeniedApproval.html", GlobalConstants.EmailTypes.DraftAssetDeniedApproval);
            }
        }

        private static void RemoveTemplate(GlobalConstants.EmailTypes emailType)
        {
            IoC.Container.GetInstance<IWorkContext>().S.EmailService.Delete("", emailType);
        }

        private static void SaveTemplate(string fileName, GlobalConstants.EmailTypes emailType)
        {
            RemoveTemplate(emailType);
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var data = File.ReadAllText(path + @"EmailTemplates/" + fileName);
                var template = new EmailTemplate()
                {
                    PortalId = "", // Base templates do not get a portalId
                    TemplateType = emailType.ToString(),
                    Data = data
                };
                IoC.Container.GetInstance<IWorkContext>().S.EmailService.Save(template);
        }

        internal static void CheckIndexes()
        {
            var siteCollection = IoC.Container.GetInstance<ISiteService>().Repository.GetQuery<Site>();
            var portalTag = new IndexKeysBuilder<Site>().Ascending(t => t.SiteSettings.PortalTag); 
 			var unique = new IndexOptionsBuilder().SetUnique(true);
            siteCollection.CreateIndex(portalTag, unique);         
        }

        private async static Task<PortalUser> TryAddUser(Site site, string userName, string email, string password, string role, string firstName, string lastName, string phone, Address address)
        {
            var userStore = new UserStore<PortalUser>(ApplicationIdentityContext.Create());
            var userManager = new PortalUserService(userStore);
            var saUser = userStore.Users.FirstOrDefault(x => x.UserName == userName);
            if (saUser == null)
            {
                saUser = new PortalUser() { UserName = email, Email = email, FirstName = firstName, LastName = lastName, PhoneNumber = phone, Address = address };
                var result = await userManager.CreateAsync(saUser, password);
                if (result.Succeeded)
                {
                    userManager.SetLockoutEnabled(saUser.Id, false);
                    userManager.AddToRole(saUser.Id, role);
                    await userManager.AddUserToSite(saUser.Id, site);
                }
            }
            return saUser;
        }
    }
}
