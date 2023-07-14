using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using HGP.Web.Models.Requests;

namespace HGP.Web.Models.Admin
{
    public class AdminSiteSettingsModel : AdminPageModel
    {
        public string SiteId { get; set; }
        public string PortalTag { get; set; } // Unique per client!
        public string HomePageMessage { get; set; }
        public string RegistrationMessage { get; set; }
        public string CustomCss { get; set; }
        public bool IsSelfRegistrationOn { get; set; }
        public bool IsOpen { get; set; }
        public string SupportEmail { get; set; }
        public string GoogleUaNumber { get; set; }
        public string Password { get; set; }
        public string PasswordMessage { get; set; }
        public MediaFileDto Logo { get; set; }
        public string CssOverride { get; set; }
        [DisplayName("Email Filter:")]
        public string EmailFilter { get; set; }
        public int? AssetExpirationDays { get; set; }


        public string HomePageMessageNoHtml { get; set; }
        public string RegistrationMessageNoHtml { get; set; }
        public string CustomCssPreview { get; set; }

        [Display(Name = "Allow self-selected approvers")]
        public bool AllowSelfSelectedApprovers { get; set; }
        public string ApprovingManagerName { get; set; }
        public string ApprovingManagerEmail { get; set; }
        public string ApprovingManagerPhone { get; set; }

        public List<ApprovalStepDto> ApprovalSteps { get; set; }

    }
}