using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class DraftAssetDeniedEmailModel : EmailModel
    {
        public string DraftAssetHitNumber { get; set; }

        public DraftAsset DraftAsset { get; set; }

        public string DraftsURL { get; set; }

        public string PrimaryImageUrl { get; set; }

        public DraftAssetDeniedEmailModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {

        }
    }
}
