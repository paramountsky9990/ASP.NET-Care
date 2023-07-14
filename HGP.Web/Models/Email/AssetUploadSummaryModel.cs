using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class AssetUploadSummaryModel : EmailModel
    {
        public List<AssetsUploaded> AssetsUploaded { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName  { get; set; }
        internal AssetUploadSummaryModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {

        }
    }


    public class AssetsUploaded
    {
        public Asset Asset { get; set; }
        public string PrimaryImageURL { get; set; }
        public string AssetURL { get; set; }
    }

}