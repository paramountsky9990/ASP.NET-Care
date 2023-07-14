using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class RequestAssetNotAvailableModel : EmailModel
    {
        public Request Request { get; set; }
        public Asset Asset { get; set; }
        public string PrimaryImageURL { get; set; }
        internal RequestAssetNotAvailableModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {

        }
    }
}