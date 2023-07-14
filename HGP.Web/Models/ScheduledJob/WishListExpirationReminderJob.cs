using AspNet.Identity.MongoDB;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Services;
using MongoDB.Driver;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using static HGP.Common.GlobalConstants;

namespace HGP.Web.Models.ScheduledJob
{
    public class WishListExpirationReminderJob : IJob
    {
        public static ILogger Logger { get; set; }

        public ISiteService SiteService = IoC.Container.GetInstance<ISiteService>();
        public IWishListService WishListService = IoC.Container.GetInstance<IWishListService>();
        public IEmailService EmailService = IoC.Container.GetInstance<IEmailService>();
        public IUnsubscribeService UnsubscribeService = IoC.Container.GetInstance<IUnsubscribeService>();

        public async Task Execute(IJobExecutionContext context)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("Job-Scheduler for WishList's Expiration Reminder");

            #region WishListExpirationReminderJob

            try
            {
                var client = new MongoClient(WebConfigurationManager.AppSettings["MongoDbConnectionString"]);
                var database = client.GetServer().GetDatabase(WebConfigurationManager.AppSettings["MongoDbName"]);
                var users = (database.GetCollection<IdentityUser>("PortalUsers"));
                var roles = database.GetCollection<IdentityRole>("Roles");
                var userStore = new UserStore<PortalUser>(new ApplicationIdentityContext(users, roles));
                var userManager = new PortalUserService(userStore);

                // EXPIRATION-REMINDER-ON, days before expiration of WishList for sending Reminder Email to User
                Int32 ExpirationReminderOn = Convert.ToInt32(WebConfigurationManager.AppSettings["ExpirationReminderOn"]); //7;

                // All Portal-Users
                List<PortalUser> allPortalUsers = userManager.Users.ToList();

                // Current Context
                HttpContext cContext = (HttpContext)(context.JobDetail.JobDataMap["cContext"]);

                //All Portals/Sites
                List<Site> allSites = SiteService.Repository.GetAll<Site>()
                    .Where(s => s.SiteSettings.IsAdminPortal == false)
                    .ToList();

                if (allSites != null && allSites.Count > 0)
                {
                    foreach (Site site in allSites)
                    {
                        //All Users in the Current Portal
                        List<PortalUser> PortalUsers = allPortalUsers.Where(u => u.PortalId == site.Id).ToList();
                        if (PortalUsers != null && PortalUsers.Count > 0)
                        {
                            foreach (PortalUser user in PortalUsers)
                            {
                                // Active WishList of the PORTAL-USER
                                List<WishList> userWishLists = WishListService.GetUserWishLists(site.Id, user.Id);
                                if (userWishLists != null && userWishLists.Count > 0)
                                {
                                    foreach (WishList wishList in userWishLists)
                                    {
                                        // Get WishLIst which will expire after {{ EXPIRATION-REMINDER-ON }} Days
                                        List<WishList> soonToBeExpiringWishLists = this.WishListService.GetSoonToBeExpiring(site.Id, user.Id, ExpirationReminderOn);
                                        if (soonToBeExpiringWishLists != null && soonToBeExpiringWishLists.Count > 0)
                                        {
                                            // Get WishLists for which user has NOT opted for not-receiving any mail
                                            List<WishList> sendMailsoonToBeExpiring = new List<WishList>();
                                            foreach (WishList smWishList in soonToBeExpiringWishLists)
                                            {
                                                if (smWishList.SendMail)
                                                {
                                                    sendMailsoonToBeExpiring.Add(smWishList);
                                                }
                                            }
                                            if (sendMailsoonToBeExpiring.Count > 0)
                                            {
                                                // Mail will be sent only if User has not Unsubscribed from it
                                                if (UnsubscribeService.GetByPortalIdUserIdUserEmail(site.Id, user.Id, user.Email).MailType == UnsubscribeTypes.ReceiveAll)
                                                {
                                                    // Send Email
                                                    this.EmailService.SendSoonToBeExpiringWishLists(cContext, site, user, sendMailsoonToBeExpiring);
                                                }
                                            }
                                        }
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