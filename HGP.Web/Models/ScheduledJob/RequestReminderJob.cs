
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Models.Email;
using HGP.Web.Services;
using Quartz;
using static HGP.Common.GlobalConstants;

namespace HGP.Web.Models.ScheduledJob
{
    // Job for sending Remainder Mails to the Approvers for the Request that are waiting for more than three days
    public class RequestReminderJob : IJob
    {
        public static ILogger Logger { get; set; }

        public ISiteService SiteService = IoC.Container.GetInstance<ISiteService>();
        public IRequestService RequestService = IoC.Container.GetInstance<IRequestService>();
        public IEmailService EmailService = IoC.Container.GetInstance<IEmailService>();
        public IAssetService AssetService = IoC.Container.GetInstance<IAssetService>();
        public IUnsubscribeService UnsubscribeService = IoC.Container.GetInstance<IUnsubscribeService>();

        public async Task Execute(IJobExecutionContext context)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("Job-Scheduler for Pending Request Reminder");

            #region RequestReminderJob

            try
            {
                // WAITING-DAYS, after which Reminder Email is sent to Approver
                Int32 waitingDays = Convert.ToInt32(WebConfigurationManager.AppSettings["waitingDays"]); //-3;

                // Updating Pending Requests Reminder-Data from the Last Reminder-Data
                PendingRequestReminderData reminderData = (PendingRequestReminderData)(context.JobDetail.JobDataMap["reminderData"]);
                Int32 sendReminderAfter = Math.Abs(waitingDays);// 3;

                // Current Context
                HttpContext cContext = (HttpContext)(context.JobDetail.JobDataMap["cContext"]);

                //All Portals/Sites
                List<Site> allSites = SiteService.Repository.GetAll<Site>().Where(s => s.SiteSettings.IsAdminPortal == false).ToList();
                if (allSites != null && allSites.Count > 0)
                {
                    foreach (Site site in allSites)
                    {
                        #region Get Pending Requests Site-Wise & Email

                        //All Requests Pending from more than 3 days
                        List<Request> pendingRequests = RequestService.GetPendingRequests(waitingDays, site);

                        // Remove data older than the sendReminderAfter-Days from the Reminder-Data Object..
                        var removeLastReminderData = reminderData.SitePendingRequests.
                            FindAll(s => s.site == site.Id && s.reminderDate.AddDays(sendReminderAfter).Date == DateTime.Now.Date);

                        if (removeLastReminderData != null && removeLastReminderData.Count > 0)
                        {
                            foreach (SitePendingRequests dataObj in removeLastReminderData)
                            {
                                reminderData.SitePendingRequests.Remove(dataObj);
                            }
                        }

                        // Remove Pending-Requests for which email is being sent three days ago(once-in-a-three-days)
                        if (pendingRequests != null && pendingRequests.Count > 0)
                        {
                            List<Request> ignorePendingReqs = new List<Request>();
                            var ignoreReqIDsList = reminderData.SitePendingRequests.Where(s => s.site == site.Id).Select(s => s.requestIDs);
                            if ((ignoreReqIDsList != null) && (ignoreReqIDsList.Count() > 0))
                            {
                                foreach (var ignoreReqIDs in ignoreReqIDsList)
                                {
                                    if (ignoreReqIDs != null && ignoreReqIDs.Count > 0)
                                    {
                                        foreach (string reqID in ignoreReqIDs)
                                        {
                                            Request ignoreReq = pendingRequests.Where(s => s.Id == reqID).FirstOrDefault();
                                            if (ignoreReq != null)
                                            {
                                                ignorePendingReqs.Add(ignoreReq);
                                            }
                                        }
                                    }
                                }
                            }

                            if (ignorePendingReqs != null && ignorePendingReqs.Count > 0)
                            {
                                foreach (Request req in ignorePendingReqs)
                                {
                                    pendingRequests.Remove(req);
                                }
                            }
                        }

                        if (pendingRequests != null && pendingRequests.Count > 0)
                        {
                            SitePendingRequests siteReqs = new SitePendingRequests();
                            siteReqs.site = site.Id;
                            siteReqs.reminderDate = DateTime.Now;
                            siteReqs.requestIDs = new List<string>();

                            foreach (Request req in pendingRequests)
                            {
                                siteReqs.requestIDs.Add(req.Id);

                                PendingRequestReminderModel pendingReqReminderModel = new PendingRequestReminderModel(cContext, site.SiteSettings);

                                pendingReqReminderModel.Request = req;
                                pendingReqReminderModel.PortalTag = site.SiteSettings.PortalTag;

                                // Get All Pending Assets of the Pending Request                           
                                pendingReqReminderModel.PendingRequestAssets = AssetService.GetPendingRequestAssets(req, site.SiteSettings.PortalTag, cContext);

                                // Get Inbox-URL
                                var baseUrl = cContext.Request.Url.Authority;
                                if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("::1"))
                                {
                                    baseUrl = WebConfigurationManager.AppSettings["JobSchedulerLocalhost"];
                                }
                                var controllerURL = "/" + site.SiteSettings.PortalTag + "/inbox";
                                pendingReqReminderModel.InboxURL = "https://" + baseUrl + controllerURL;

                                string unsubscribeURL = string.Empty;
                                string unsubControllerURL = "/" + site.SiteSettings.PortalTag + "/unsubscribe";
                                unsubscribeURL = "https://" + baseUrl + unsubControllerURL;

                                pendingReqReminderModel.UnsubscribeURL = unsubscribeURL;


                                // Days a request waited after the  WAITING-DAYS
                                pendingReqReminderModel.DaysReqWaited = (DateTime.Now - req.RequestDate).Days;

                                // Mail will be sent only if User has not Unsubscribed from it
                                if (UnsubscribeService.GetByPortalIdUserIdUserEmail(site.Id, req.RequestorId, req.RequestorEmail).MailType == UnsubscribeTypes.ReceiveAll)
                                {
                                    // Reminder-Email to Approver for Pending Request
                                    EmailService.SendPendingRequestReminder(pendingReqReminderModel);
                                }
                            }

                            reminderData.SitePendingRequests.Add(siteReqs);

                        }

                        #endregion                     

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