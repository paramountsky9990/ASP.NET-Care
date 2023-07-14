using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class ForgotPasswordEmailModel : EmailModel
    {
        public string CallbackUrl { get; set; }
        public string Code { get; set; }
        public PortalUser User { get; set; }
        public Site Site { get; set; }
        internal ForgotPasswordEmailModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {

        }
    }
}