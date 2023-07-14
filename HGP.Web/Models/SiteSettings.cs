
using System;
using System.Collections.Generic;
using System.Web.Configuration;
using HGP.Web.DependencyResolution;
using HGP.Web.Models.Requests;
using HGP.Web.Services;
using MongoDB.Bson.Serialization.Attributes;

namespace HGP.Web.Models
{
    public class SiteSettings
    {
        public bool IsSelfRegistrationOn { get; set; }
        public string HomePageMessage { get; set; }
        public string RegistrationMessage { get; set; }
        public string EmailFilter { get; set; }
        public string CustomCss { get; set; }
        public bool IsConfigured { get; set; }
        public bool IsOpen { get; set; }
        public string PortalTag { get; set; } // Unique per client!
        public string CompanyName { get; set; }
        public string SupportEmail { get; set; }
        public string Phone { get; set; }
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public int TimezoneOffset { get; set; }
        public int TimezoneId { get; set; }
        public int CurrencyId { get; set; }
        public int LanguageId { get; set; }
        public string GoogleUaNumber { get; set; }
        public string Password { get; set; }
        public string PasswordMessage { get; set; }
        public MediaFileDto Logo { get; set; }
        public DateTime? LastLogin { get; set; }

        public int NextRequestNum { get; set; }
        public string LastRequestFormat { get; set; }
        public string LastRequestBase { get; set; }

        public bool AllowSelfSelectedApprovers  { get; set; }
        public string ApprovingManagerName { get; set; }
        public string ApprovingManagerEmail { get; set; }
        public string ApprovingManagerPhone { get; set; }
        public bool IsAdminPortal { get; set; }
        public IList<ApprovalStep> ApprovalSteps { get; set; }
        public IList<string> ApprovalCcAddresses { get; set; }

        public string BookValueMessage { get; set; }
        public string RequestPageMessage { get; set; }
        /// <summary>
        /// Allowed values: selfupload, wishlist, allowmultiplerequests
        /// </summary>
        public IList<string> Features { get; set; }
        public bool UseAssetOwnerForApproval { get; set; }

        [BsonIgnore]
        // Used to locate static images for emails (logo for example)
        public string StaticContentPath { get; set; } = WebConfigurationManager.AppSettings["StaticContentPath"];
        [BsonIgnore]
        // Used to locate a portal's asset images for the website
        public string BaseImagesPath { get; set; } = WebConfigurationManager.AppSettings["BaseImagesPath"];


        public SiteSettings()
        {
            this.IsSelfRegistrationOn = true;
            this.IsConfigured = false;
            this.IsOpen = true;
            this.HomePageMessage = "<p class=\"lead\">Welcome to CARE!</p><p>Heritage Global Partners is proud to offer our clients a state of the art internal redeployment software solution.</p><p><a href=\"https://www.hgpauction.com/care/\" class=\"btn btn-primary btn-lg\">Learn more &raquo;</a></p>";
            this.RegistrationMessage = "Create a new account";
            this.NextRequestNum = 100000;
            this.LastRequestFormat = "{0}-{1:N0}"; // todo: Suport custom formats
            LastRequestBase = "aa";
            this.ApprovalSteps = IoC.Container.GetInstance<SiteService>().GetDefaultProcess();
            this.ApprovalCcAddresses = new List<string>();
            this.Features  = new List<string>();
            this.UseAssetOwnerForApproval = false;
        }

    }
}
