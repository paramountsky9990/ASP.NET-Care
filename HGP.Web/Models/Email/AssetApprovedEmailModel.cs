using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class AssetApprovedEmailModel : EmailModel
    {
        public string RequestId { get; set; }
        public string RequestNum { get; set; }

        public string ApprovedByName { get; set; }
        public string ApprovedByEmail { get; set; }
        public string ApprovedByPhone { get; set; }
        public string ApprovedByLocation { get; set; }
        public DateTime ApprovedByDate { get; set; }

        //public string Notes { get; set; }

        public AssetRequestDetail Asset { get; set; }
        public string AssetURL { get; set; }
        public Request Request { get; set; }
        public AssetRequestEmailDto AssetRequestDetail { get; set; }

        public string PrimaryImageUrl { get; set; }

        internal AssetApprovedEmailModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {
            
        }

    }
}
