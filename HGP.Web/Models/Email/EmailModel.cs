using System.Web;
using System.Web.Configuration;
using AutoMapper;

namespace HGP.Web.Models.Email
{

    public class EmailModel
    {
        public string PortalId { get; set; }
        public string ToAddress { get; set; }
        public string CcAddress { get; set; }
        public string CcName { get; set; }
        public string ToName { get; set; }
        public string FromAddress { get; set; }
        public string FromName { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public string SMTPServer { get; set; }
        public string SMTPUserName { get; set; }
        public string SMTPPassword { get; set; }
        public bool UseCredentials { get; set; }
        public string UnsubscribeURL { get; set; }
        public string BasePortalURL { get; set; }
        public string StaticContentPath { get; set; }
        public SiteSettings SiteSettings { get; set; }
        

        protected EmailModel(HttpContext context, SiteSettings siteSettings)
        {
            this.FromAddress = "caresupport@hgpauction.com";
            this.FromName = "CARE";

            string baseUrl = context.Request.Url.Authority;
            if (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("::1"))
            {
                baseUrl = WebConfigurationManager.AppSettings["JobSchedulerLocalhost"];
            }

            string unsubControllerURL = "/" + siteSettings.PortalTag + "/unsubscribe";
            this.UnsubscribeURL = "https://" + baseUrl + unsubControllerURL;
            this.BasePortalURL = "https://" + baseUrl + "/" + siteSettings.PortalTag;

            this.FromAddress = WebConfigurationManager.AppSettings["FromAddress"];
            this.FromName = WebConfigurationManager.AppSettings["FromName"];
            this.SMTPServer = WebConfigurationManager.AppSettings["SMTPServer"];
            this.SMTPUserName = WebConfigurationManager.AppSettings["SMTPUserName"];
            this.SMTPPassword = WebConfigurationManager.AppSettings["SMTPPassword"];
            this.UseCredentials = bool.Parse(WebConfigurationManager.AppSettings["SMTPUseCredentials"]);
            this.StaticContentPath = WebConfigurationManager.AppSettings["StaticContentPath"];
            this.SiteSettings = siteSettings;

        }


    }
}
