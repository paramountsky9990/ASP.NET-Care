using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class PendingRequestReminderModel : EmailModel
    {       
        public Request Request { get; set; }
        public List<PendingRequestAssets> PendingRequestAssets { get; set; }
        public string InboxURL { get; set; }
        public int DaysReqWaited { get; set; }
        public string PortalTag { get; set; }

        internal PendingRequestReminderModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {

        }
    }

    public class PendingRequestAssets
    {
        public Asset Asset { get; set; }
        public string PrimaryImageURL { get; set; }
    }


}