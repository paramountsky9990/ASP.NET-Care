using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class AssetDeniedEmailModel : EmailModel
    {
        public string RequestId { get; set; }
        public string RequestNum { get; set; }

        public string ApprovingManagerName { get; set; }
        public string ApprovingManagerEmail { get; set; }
        public string ApprovingManagerPhone { get; set; }
        public DateTime DeniedByDate { get; set; }

       // public string Notes { get; set; }

        public AssetRequestDetail Asset { get; set; }
        public Request Request { get; set; }

        public AssetRequestEmailDto AssetRequestDetail { get; set; }
        public string AssetURL { get; set; }

        public string PrimaryImageUrl { get; set; }

        internal AssetDeniedEmailModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {

        }
    }
}
