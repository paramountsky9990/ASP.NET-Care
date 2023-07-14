using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;
using HGP.Common.Database;

namespace HGP.Web.Models.InBox
{
    public class InboxItem : MongoObjectBase
    {
        public string PortalId { get; set; }
        public string OwnerId { get; set; }
        public bool IsRead { get; set; }
        public GlobalConstants.InboxStatusTypes Status { get; set; }
        public GlobalConstants.InboxItemTypes Type { get; set; }
        public Request Data { get; set; }
    }
}