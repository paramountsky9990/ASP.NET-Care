using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Settings;
using HGP.Web.Services;
using HGP.Web.Utilities;

namespace HGP.Web.Controllers
{
    [Authorize]
    public class SettingsController : BaseController
    {
        // GET: Settings
        public static ILogger Logger { get; set; }

        public SettingsController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("SettingsController");
        }

        // GET: List
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index()
        {
            var url = Url.RouteUrl("PortalRoute", new { controller = "Account", action = "Register" }, protocol: Request.Url.Scheme);
            var model = this.S.SiteService.BuildSiteSettingsHomeModel(this.S.WorkContext.CurrentSite.Id, url);
            return View(model);
        }

        [HttpGet]
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult GetHomePageMessage(string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);

                return Json(new { success = true, message = site.SiteSettings.HomePageMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult EditHomePageMessage(string newMessage, string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);
                site.SiteSettings.HomePageMessage = newMessage.Trim();
                this.S.SiteService.Save(site);

                var previewStr = "";
                if (!string.IsNullOrWhiteSpace(site.SiteSettings.HomePageMessage))
                    previewStr = Regex.Replace(site.SiteSettings.HomePageMessage, @"<[^>]*>", String.Empty);
                return Json(new { success = true, message = "Site home page message changed.", preview = previewStr });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult EditBookValueMessage(string newMessage, string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);
                site.SiteSettings.BookValueMessage = newMessage.Trim();
                this.S.SiteService.Save(site);

                var previewStr = "";
                if (!string.IsNullOrWhiteSpace(site.SiteSettings.BookValueMessage))
                    previewStr = Regex.Replace(site.SiteSettings.BookValueMessage, @"<[^>]*>", String.Empty);
                return Json(new { success = true, message = "Book value message changed.", preview = previewStr });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult EditRequestPageMessage(string newMessage, string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);
                site.SiteSettings.RequestPageMessage = newMessage.Trim();
                this.S.SiteService.Save(site);

                var previewStr = "";
                if (!string.IsNullOrWhiteSpace(site.SiteSettings.RequestPageMessage))
                    previewStr = Regex.Replace(site.SiteSettings.RequestPageMessage, @"<[^>]*>", String.Empty);
                return Json(new { success = true, message = "Request page message changed.", preview = previewStr });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult GetRegistrationMessage(string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);

                return Json(new { success = true, message = site.SiteSettings.RegistrationMessage }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult EditRegistrationMessage(string newMessage, string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);
                site.SiteSettings.RegistrationMessage = newMessage.Trim();
                this.S.SiteService.Save(site);

                var previewStr = "";
                if (!string.IsNullOrWhiteSpace(site.SiteSettings.RegistrationMessage))
                    previewStr = Regex.Replace(site.SiteSettings.RegistrationMessage, @"<[^>]*>", String.Empty);
                return Json(new { success = true, message = "Site registration message changed.", preview = previewStr });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult GetCustomCss(string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);

                return Json(new { success = true, message = site.SiteSettings.CustomCss }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult EditCustomCss(string newCss, string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);
                site.SiteSettings.CustomCss = newCss.Trim();
                this.S.SiteService.Save(site);

                var previewStr = "";
                if (!string.IsNullOrWhiteSpace(site.SiteSettings.CustomCss))
                    previewStr = Regex.Replace(site.SiteSettings.CustomCss.Left(100), @"<[^>]*>", String.Empty);
                return Json(new { success = true, message = "Custom Css changed.", preview = previewStr });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult GetEmailFilter(string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);

                return Json(new { success = true, message = site.SiteSettings.EmailFilter }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult EditEmailFilter(string emailFilter, string siteId)
        {
            try
            {
                emailFilter = emailFilter.StripWhiteSpace();
                if (!IsValid(emailFilter))
                    return Json(new { success = false, message = "Invalid filter" });

                var site = this.S.SiteService.GetById(siteId);
                site.SiteSettings.EmailFilter = emailFilter;
                this.S.SiteService.Save(site);

                return Json(new { success = true, message = "Email filter changed.", preview = site.SiteSettings.EmailFilter });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpGet]
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult GetDefaultApprover(string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);

                return Json(new { success = true, allowSelfSelectedApprovers = site.SiteSettings.AllowSelfSelectedApprovers,
                                                    approvingManagerName = site.SiteSettings.ApprovingManagerName,
                                                    approvingManagerEmail = site.SiteSettings.ApprovingManagerEmail,
                                                    approvingManagerPhone = site.SiteSettings.ApprovingManagerPhone
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult EditDefaultApprover(bool allowSelfSelectedApprovers, string approvingManagerName, string approvingManagerEmail, string approvingManagerPhone, string siteId)
        {
            try
            {
                approvingManagerEmail = approvingManagerEmail.StripWhiteSpace();
                approvingManagerPhone = Utilities.StringExtensions.StripPunctuation(approvingManagerPhone.StripWhiteSpace());

                if (!allowSelfSelectedApprovers)
                {
                    var errorMessage = "";
                    if (!IsValidEmail(approvingManagerEmail))
                        errorMessage += "Invalid email address. ";
                    if (string.IsNullOrEmpty(approvingManagerName))
                        errorMessage += "Invalid approver's name. ";
                    if (string.IsNullOrEmpty(approvingManagerPhone))
                        errorMessage += "Invalid phone number. ";

                    if (!string.IsNullOrEmpty(errorMessage))
                        return Json(new { success = false, message = errorMessage });
                }

                var site = this.S.SiteService.GetById(siteId);
                site.SiteSettings.AllowSelfSelectedApprovers = allowSelfSelectedApprovers;
                site.SiteSettings.ApprovingManagerName = approvingManagerName;
                site.SiteSettings.ApprovingManagerEmail = approvingManagerEmail;
                site.SiteSettings.ApprovingManagerPhone = approvingManagerPhone;
                this.S.SiteService.Save(site);

                return Json(new { success = true, message = "Approver settings changed.",
                    allowSelfSelectedApprovers = site.SiteSettings.AllowSelfSelectedApprovers,
                    approvingManagerName = site.SiteSettings.ApprovingManagerName,
                    approvingManagerEmail = site.SiteSettings.ApprovingManagerEmail,
                    approvingManagerPhone = site.SiteSettings.ApprovingManagerPhone
                });
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        [HttpGet]
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult GetApproverCcList(string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);

                return Json(new
                {
                    success = true,
                    addresses = site.SiteSettings.ApprovalCcAddresses
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult EditApproverCcList(string[] emailAddresses, string siteId)
        {
            try
            {
                var site = this.S.SiteService.GetById(siteId);
                if (emailAddresses == null)
                    site.SiteSettings.ApprovalCcAddresses = new List<string>();
                else
                    site.SiteSettings.ApprovalCcAddresses = emailAddresses.OrderBy(x => x).ToList();
                this.S.SiteService.Save(site);

                return Json(new { success = true, message = "Approval email addresses have been updated.", addresses = site.SiteSettings.ApprovalCcAddresses });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool IsValid(string value)
        {
            bool result = false;

            if (value == null)
            {
                return true;
            }

            if (string.IsNullOrEmpty(value))
            {
                result = true;
            }
            else
            {
                char[] delimiters = new char[2] { ';', ',' };
                var domainList = value.Split(delimiters);

                result = domainList.All(IsValidEmail);
            }

            return result;
        }

        private bool IsValidEmail(string emailAddress)
        {
            const string pattern = @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,6}(\n|$)";
            var result = Regex.IsMatch(emailAddress, pattern);
            return result;

        }

    }
}