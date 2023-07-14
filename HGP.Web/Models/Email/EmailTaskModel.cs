using System;
using System.Collections.Generic;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class EmailTaskModel : EmailModel
    {
        public string Id { get; set; }
        public string RequestId { get; set; }
        public int RequestTaskTypeId { get; set; }
        public string GroupsToPerform { get; set; }
        public int? UserToPerform { get; set; }
        public DateTime? CompletedDate { get; set; }
        public int? CompletedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        public string RequestNum { get; set; }
        public string RequestorName { get; set; }
        public string RequestorEmail { get; set; }
        public string RequestorPhone { get; set; }
        public Address ShipToAddress { get; set; }
        public DateTime RequestDate { get; set; }

        public AssetRequestEmailDto AssetRequestDetail { get; set; }
        public Request Request { get; set; }
       // public string Notes { get; set; }

        public string ApprovalURL { get; set; }
        public string AssetURL { get; set; }

        public string PrimaryImageUrl { get; set; }
        public DraftAsset DraftAsset { get; set; }

        internal EmailTaskModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {

        }
    }
}



