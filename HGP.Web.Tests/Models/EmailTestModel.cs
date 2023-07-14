using System;
using System.Web;
using System.Web.Configuration;

namespace HGP.Web.Models.Email
{
    public class EmailTestModel : EmailModel
    {
        public EmailTestModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {
            ToAddress = "rrb@matrix6.com";
            ToName = "Rick Boarman";
            FromAddress = WebConfigurationManager.AppSettings["FromAddress"];
            FromName = WebConfigurationManager.AppSettings["FromName"];
            Message = "This is a test " + DateTime.Now.ToShortTimeString();
            SMTPServer = WebConfigurationManager.AppSettings["SMTPServer"];
            SMTPUserName = WebConfigurationManager.AppSettings["SMTPUserName"];
            SMTPPassword = WebConfigurationManager.AppSettings["SMTPPassword"];
            UseCredentials = bool.Parse(WebConfigurationManager.AppSettings["SMTPUseCredentials"]);
        }

        public AssetRequestEmailDto AssetModel { get; set; }


    }
}