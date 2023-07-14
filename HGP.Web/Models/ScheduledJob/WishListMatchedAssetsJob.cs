using AspNet.Identity.MongoDB;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Models.Email;
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
    public class WishListMatchedAssetsJob : IJob
    {
        public static ILogger Logger { get; set; }

        public ISiteService SiteService = IoC.Container.GetInstance<ISiteService>();
        public IAssetService AssetService = IoC.Container.GetInstance<IAssetService>();
        public IWishListService WishListService = IoC.Container.GetInstance<IWishListService>();
        public IMatchedAssetService MatchedAssetService = IoC.Container.GetInstance<IMatchedAssetService>();
        public IEmailService EmailService = IoC.Container.GetInstance<IEmailService>();
        public IUnsubscribeService UnsubscribeService = IoC.Container.GetInstance<IUnsubscribeService>();

        public async Task Execute(IJobExecutionContext context)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("Job-Scheduler for WishList's Matched Assets");

            #region WishListMatchedAssetsJob
            try
            {

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

                //All Portals/Sites
                List<Site> allSites = SiteService.Repository.GetAll<Site>().Where(s => s.SiteSettings.IsAdminPortal == false).ToList();
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
                                        // Assets matched to WishList which are visible
                                        List<Asset> assetsMatched = AssetService.GetWishListAssetsMatched(site, wishList);
                                        List<Asset> assetsMatchedToSentEmail = new List<Asset>();
                                        List<string> matchedAssetsIDList = new List<string>();

                                        if (assetsMatched != null && assetsMatched.Count > 0)
                                        {
                                            // loop each matched-Assets to check if Email is being sent for this asset
                                            foreach (Asset asset in assetsMatched)
                                            {
                                                MatchedAsset matchedAsset = MatchedAssetService.GetByWishListIDAndAssetID(wishList.Id, asset.Id);
                                                if (matchedAsset != null)
                                                {
                                                    // Send Mail to Matched-Assets with status : Matched and  if already a Mail is not sent for this
                                                    if ((!(matchedAsset.IsEmailSent)) && (matchedAsset.Status == MatchedAssetStatusTypes.Matched))
                                                    {
                                                        matchedAssetsIDList.Add(matchedAsset.Id);
                                                        // add to assetsMatchedToSentEmail list
                                                        assetsMatchedToSentEmail.Add(asset);
                                                    }
                                                }
                                                else
                                                {
                                                    // Create MAtchedAsset-Object
                                                    string matchedAssetID = MatchedAssetService.Add(wishList.Id, asset.Id);
                                                    if (!(string.IsNullOrEmpty(matchedAssetID)))
                                                    {
                                                        matchedAssetsIDList.Add(matchedAssetID);
                                                        // add to assetsMatchedToSentEmail list
                                                        assetsMatchedToSentEmail.Add(asset);
                                                    }
                                                }
                                            }

                                            if (assetsMatchedToSentEmail != null && assetsMatchedToSentEmail.Count > 0)
                                            {
                                                // make assetsMatchedToSentEmail to Assetpload to get image & url
                                                List<AssetsUploaded> detailedMatchedAssets = AssetService.GetWishListDetailedMatchedAssets(site, cContext, assetsMatchedToSentEmail);
                                                if (detailedMatchedAssets != null & detailedMatchedAssets.Count > 0)
                                                {
                                                    // Check if User has manually opted for not receiving Mail for this Wishlist
                                                    if (wishList.SendMail)
                                                    {
                                                        // Mail will be sent only if User has not Unsubscribed from it
                                                        if (UnsubscribeService.GetByPortalIdUserIdUserEmail(site.Id, user.Id, user.Email).MailType == UnsubscribeTypes.ReceiveAll)
                                                        {
                                                            // Send Email for Wishlist Matched Assets
                                                            EmailService.SendWishListMatchedAssets(user, detailedMatchedAssets, wishList, cContext, site);
                                                        }
                                                    }
                                                }
                                                // update Wishlist's Matched Assets , IsEmailSent=True
                                                foreach (string matchedAssetID in matchedAssetsIDList)
                                                {
                                                    MatchedAssetService.UpdateEmailSent(matchedAssetID);
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