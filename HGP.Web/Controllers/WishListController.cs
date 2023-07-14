using HGP.Web.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HGP.Common.Logging;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.WishLists;
using HGP.Web.Models.Email;
using static HGP.Common.GlobalConstants;

namespace HGP.Web.Controllers
{
    [Authorize]
    public class WishListController : BaseController
    {
        public static ILogger Logger { get; set; }

        public WishListController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("WishListController");
        }

        // GET: WishList
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index()
        {
            var model = this.S.WishListService.BuildWishListHomeModel(this.S.WorkContext.CurrentSite.Id, this.S.WorkContext.CurrentUser.Id);
            return View(model);
        }

        [HttpPost]
        public ActionResult Create(string searchCriteria)
        {
            bool res = false;
            string message = string.Empty;
            try
            {
                if (!(this.S.WishListService.IsWishListExists(searchCriteria)))
                {
                    res = this.S.WishListService.AddWishList(searchCriteria);
                    if (res)
                        DisplaySuccessMessage("1 wish successfully added.");
                    else
                        DisplayErrorMessage("An error occured. Please try again later.");
                }
                else
                    DisplayErrorMessage("This wish already exists.");

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }
            return Json(new { success = res });

        }

        [HttpPost]
        public ActionResult Edit(string wishListID, string searchCriteria)
        {
            bool res = false;
            try
            {
                if (!(this.S.WishListService.IsWishListExists(searchCriteria)))
                {
                    res = this.S.WishListService.EditWishList(wishListID, searchCriteria);
                    if (res)
                        DisplaySuccessMessage("1 wish successfully updated.");
                    else
                        DisplayErrorMessage("An error occured. Please try again later.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }
            return Json(new { success = res });
        }

        [HttpPost]
        public ActionResult Delete(string wishListID)
        {
            bool res = false;
            try
            {
                if (!(string.IsNullOrEmpty(wishListID)))
                {
                    res = this.S.WishListService.DeleteWishList(wishListID);
                    if (res)
                        DisplaySuccessMessage("1 wish successfully removed.");
                    else
                        DisplayErrorMessage("An error occured. Please try again later.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }

            return Json(new { success = res });
        }

        public ActionResult Extend(string wishListID, bool isAutoExtend = false)
        {
            bool res = false;
            try
            {
                res = this.S.WishListService.ExtendWishList(wishListID);
                if (res)
                    DisplaySuccessMessage("1 wish successfully extended.");
                else
                    DisplayErrorMessage("An error occured. Please try again later.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }

            if (isAutoExtend)
            {
                return RedirectToRoute("PortalRoute", new { controller = "Account", action = "Login" });
            }
            else
            {
                return RedirectToRoute("PortalRoute", new { controller = "WishList", action = "Index" });
            }

        }

        public ActionResult WishListResult(string wishListID)
        {
            WishListResultHomeModel model = new WishListResultHomeModel();
            try
            {
                if (!(string.IsNullOrEmpty(wishListID)))
                {
                    model = this.S.WishListService.BuildWishListResultHomeModel(
                        this.S.WorkContext.CurrentSite.Id,
                        // this.S.WorkContext.CurrentUser.Id,
                        wishListID);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }

            return View(model);
        }

        public ActionResult SendWishListMatchedAssetEmail(string wishListID)
        {
            bool res = false;
            string message = string.Empty;
            try
            {
                if (string.IsNullOrEmpty(wishListID))
                {
                    DisplayErrorMessage("Please select the WishList.");
                }
                else
                {
                    WishList wishList = this.S.WishListService.GetById(wishListID);
                    if (wishList == null)
                    {
                        DisplayErrorMessage("Please select the WishList.");
                    }
                    else
                    {
                        Site site = (Site)this.S.WorkContext.CurrentSite;
                        HttpContext cContext = System.Web.HttpContext.Current;
                        PortalUser user = this.S.WorkContext.CurrentUser;

                        #region SendWishListMatchedAssetsEmail

                        // Assets matched to WishList which are visible
                        List<Asset> assetsMatched = this.S.AssetService.GetWishListAssetsMatched(site, wishList);
                        List<Asset> assetsMatchedToSentEmail = new List<Asset>();
                        List<string> matchedAssetsIDList = new List<string>();

                        if (assetsMatched != null && assetsMatched.Count > 0)
                        {
                            // loop each matched-Assets to check if Email is being sent for this asset
                            foreach (Asset asset in assetsMatched)
                            {
                                MatchedAsset matchedAsset = this.S.MatchedAssetService.GetByWishListIDAndAssetID(wishList.Id, asset.Id);
                                if (matchedAsset != null)
                                {
                                    if ((!(matchedAsset.IsEmailSent)) && (matchedAsset.Status == MatchedAssetStatusTypes.Matched))
                                    {
                                        matchedAssetsIDList.Add(matchedAsset.Id);
                                        // add to assetsMatchedToSentEmail list
                                        assetsMatchedToSentEmail.Add(asset);
                                    }
                                }
                                else
                                {
                                    // Create MatchedAsset-Object
                                    string matchedAssetID = this.S.MatchedAssetService.Add(wishList.Id, asset.Id);
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
                                List<AssetsUploaded> detailedMatchedAssets = this.S.AssetService.GetWishListDetailedMatchedAssets(site, cContext, assetsMatchedToSentEmail);
                                if (detailedMatchedAssets != null & detailedMatchedAssets.Count > 0)
                                {
                                    // Check if User has manually opted for not receiving Mail for this Wishlist
                                    if (wishList.SendMail)
                                    {
                                        // Send Email for Wishlist Matched Assets
                                        this.S.EmailService.SendWishListMatchedAssets(user, detailedMatchedAssets, wishList, cContext, site);

                                        DisplaySuccessMessage("Successfully sent Emails for the WishList : " + wishList.SearchCriteria);
                                    }
                                }
                                // update Wishlist's Matched Assets , IsEmailSent=True
                                foreach (string matchedAssetID in matchedAssetsIDList)
                                {
                                    this.S.MatchedAssetService.UpdateEmailSent(matchedAssetID);
                                }
                            }
                            else
                            {
                                DisplayErrorMessage("No Assets matching the WishList : " + wishList.SearchCriteria);
                            }
                            res = true;
                        }
                        #endregion                        
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }
            return Json(new { success = res });
        }

        public ActionResult IgnoreMatch(string wishListID, string assetID)
        {
            bool res = false;
            string message = string.Empty;
            try
            {
                res = this.S.WishListService.IgnoreMatchedAsset(wishListID, assetID);
                if (res)
                    DisplaySuccessMessage("1 wish successfully ignored.");
                else
                    DisplayErrorMessage("An error occured. Please try again later.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }
            return Json(new { success = res });
        }

        [HttpPost]
        public ActionResult CloseWishList(string wishListID)
        {
            bool res = false;
            string message = String.Empty;
            try
            {
                if (!(string.IsNullOrEmpty(wishListID)))
                {
                    res = this.S.WishListService.CloseWishList(wishListID);
                    if (res)
                        message = "1 wish successfully closed.";
                    else
                        message = "An error occured. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }

            return Json(new { success = res, message = message });
        }

        [HttpPost]
        public ActionResult EditSendMail(string wishListID, bool sendMail)
        {
            bool res = false;
            string message = string.Empty;
            try
            {
                if (!(string.IsNullOrEmpty(wishListID)))
                {
                    res = this.S.WishListService.EditSendMail(wishListID, sendMail);
                    if (res)
                    {
                        if (sendMail)
                        {
                            message = "You will recieve emails for this wish.";
                        }
                        else
                        {
                            message = "You will no longer recieve email for this wish.";
                        }
                    }
                    else
                    {
                        message = "An error occured. Please try again later.";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception");
            }

            return Json(new { success = res, message = message });
        }
    }
}