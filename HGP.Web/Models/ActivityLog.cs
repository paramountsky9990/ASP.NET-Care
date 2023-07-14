using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;
using HGP.Common.Database;

namespace HGP.Web.Models
{
    public class ActivityLog : MongoObjectBase
    {
        public string ActivityType { get; set; }
        public string PortalId { get; set; }
        public string UserId { get; set; }
        public string Data { get; set; }
        public string Data2 { get; set; }
    }
}