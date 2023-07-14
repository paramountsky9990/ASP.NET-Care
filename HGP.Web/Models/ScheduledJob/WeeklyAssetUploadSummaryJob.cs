using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using HGP.Common;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Models.Email;
using HGP.Web.Services;
using MongoDB.Driver;
using AspNet.Identity.MongoDB;
using Quartz;
using static HGP.Common.GlobalConstants;

namespace HGP.Web.Models.ScheduledJob
{
    // Job Created for sending Weekly Asset Upload Summary
    public class WeeklyAssetUploadSummaryJob : IJob
    {
        public static ILogger Logger { get; set; }

        public ISiteService SiteService = IoC.Container.GetInstance<ISiteService>();
        public IAssetService AssetService = IoC.Container.GetInstance<IAssetService>();
        public IEmailService EmailService = IoC.Container.GetInstance<IEmailService>();
        public IUnsubscribeService UnsubscribeService = IoC.Container.GetInstance<IUnsubscribeService>();


        public async Task Execute(IJobExecutionContext context)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("Job-Scheduler for Weekly-Asset Uploaded Summary");

            #region WeeklyAssetUploadSummaryJob
            try
            {
                Int32 intervalInDays = -7; // Weekly
                var client = new MongoClient(WebConfigurationManager.AppSettings["MongoDbConnectionString"]);
                var database = client.GetServer().GetDatabase(WebConfigurationManager.AppSettings["MongoDbName"]);
                var users = (database.GetCollection<IdentityUser>("PortalUsers"));
                var roles = database.GetCollection<IdentityRole>("Roles");
                var userStore = new UserStore<PortalUser>(new ApplicationIdentityContext(users, roles));
                var userManager = new PortalUserService(userStore);
                // All Portal-Users
                List<PortalUser> allPortalUsers = userManager.Users.ToList();

                // Current Context
                HttpContext cContext = (HttpContext)(context.JobDetail.JobDataMap["cContext"]);

                IoC.Container.GetInstance<IActivityLogService>().LogActivity(GlobalConstants.ActivityTypes.SendWeeklyAssetUploadSummary);

                //All Portals/Sites
                List<Site> allSites = SiteService.Repository.GetAll<Site>().Where(s => s.SiteSettings.IsAdminPortal == false).ToList();
                if (allSites != null && allSites.Count > 0)
                {
                    foreach (Site site in allSites)
                    {
                        //All Assets Uploaded weekly
                        List<AssetsUploaded> assetsUploaded = AssetService.GetAssetUploadSummary(site, cContext, intervalInDays);
                        if (assetsUploaded != null && assetsUploaded.Count > 0)
                        {
                            //All Users in the Current Portal
                            List<PortalUser> PortalUsers = allPortalUsers.Where(u => u.PortalId == site.Id).ToList();
                            if (allPortalUsers != null && allPortalUsers.Count > 0)
                            {
                                foreach (PortalUser user in PortalUsers)
                                {
                                    // Mail will be sent only if User has not Unsubscribed from it
                                    if (UnsubscribeService.GetByPortalIdUserIdUserEmail(site.Id, user.Id, user.Email).MailType == UnsubscribeTypes.ReceiveAll)
                                    {
                                        // Email All Assets Uploaded in Last Week
                                        EmailService.SendAssetUploadSummary(cContext, site, user, assetsUploaded);
                                    }
                                }
                            }
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