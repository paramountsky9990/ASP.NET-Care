using AspNet.Identity.MongoDB;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Services;
using MongoDB.Driver;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using Microsoft.AspNet.Identity;
using static HGP.Common.GlobalConstants;

namespace HGP.Web.Models.ScheduledJob
{
    public class AssetExpirationReminderJob : IJob
    {
        public static ILogger Logger { get; set; }

        public ISiteService SiteService = IoC.Container.GetInstance<ISiteService>();
        public IAssetService AssetService = IoC.Container.GetInstance<IAssetService>();
        public IEmailService EmailService = IoC.Container.GetInstance<IEmailService>();
        public IUnsubscribeService UnsubscribeService = IoC.Container.GetInstance<IUnsubscribeService>();

        public async Task Execute(IJobExecutionContext context)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("Job-Scheduler for Asset's Expiration Reminder");

            #region WishListExpirationReminderJob

            try
            {
                var client = new MongoClient(WebConfigurationManager.AppSettings["MongoDbConnectionString"]);
                var database = client.GetServer().GetDatabase(WebConfigurationManager.AppSettings["MongoDbName"]);
                var users = (database.GetCollection<IdentityUser>("PortalUsers"));
                var roles = database.GetCollection<IdentityRole>("Roles");
                var userStore = new UserStore<PortalUser>(new ApplicationIdentityContext(users, roles));
                var userManager = new PortalUserService(userStore);

                // Current Context
                HttpContext cContext = (HttpContext)(context.JobDetail.JobDataMap["cContext"]);

                //All Portals/Sites
                List<Site> allSites = SiteService.Repository.GetAll<Site>()
                    .Where(s => s.SiteSettings.IsAdminPortal == false)
                    .ToList();

                if (allSites.Any())
                {
                    foreach (Site site in allSites)
                    {
                        var adminUsers = new Dictionary<string, PortalUser>();
                        List<Asset> expiringAssets = AssetService.GetExpiringAssets(site.Id, DateTime.Today, DateTime.Today.AddDays(1));
                        if ((expiringAssets != null) && expiringAssets.Any())
                        {
                            var user = userManager.FindByEmail(site.AccountExecutive.Email);
                            adminUsers.Add(user.Email, user);

                            var admins = this.SiteService.GetAdmins(site.Id);
                            foreach (var portalUser in admins)
                            {
                                if (!adminUsers.ContainsKey(portalUser.Email))
                                {
                                    adminUsers.Add(portalUser.Email, portalUser);
                                }
                            }

                            // Make sure users are not sent duplicate messages
                            foreach (var portalUser in adminUsers.Values)
                                await this.EmailService.SendExpiringAssetsMessage(cContext, site, portalUser, expiringAssets);                                

                            

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }

            #endregion
        }
    }
}