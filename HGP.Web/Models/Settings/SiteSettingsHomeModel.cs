using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Settings
{
    public class SiteSettingsHomeModel : PageModel
    {
        public string Id { get; set; }
        public string PortalTag { get; set; }
        public string SiteId { get; set; }
        public string HomePageMessage { get; set; }
        public string RegistrationMessage { get; set; }
        public string CustomCss { get; set; }
        public bool IsSelfRegistrationOn { get; set; }
        public bool IsOpen { get; set; }
        public string SupportEmail { get; set; }
        public string GoogleUaNumber { get; set; }
        public string Password { get; set; }
        public string PasswordMessage { get; set; }
        public string LogoUri { get; set; }
        public string CssOverride { get; set; }


        public string HomePageMessageNoHtml { get; set; }
        public string RegistrationMessageNoHtml { get; set; }
        public string RegistrationUrl{ get; set; }
        public string CustomCssPreview { get; set; }
        [Display(Name = "Display this label when book value is not provided (30 max):")]
        public string BookValueMessage { get; set; }
        [Display(Name = "Display these instructions on the request page:")]
        public string RequestPageMessage { get; set; }
        public string RequestPageMessagesPreview { get; set; }       
    }
}