using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class WelcomeEmailModel : EmailModel
    {
        public string Url { get; set; }
        public string Password { get; set; }
        public PortalUser User { get; set; }
        public Site Site { get; set; }
        internal WelcomeEmailModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {

        }
    }
}