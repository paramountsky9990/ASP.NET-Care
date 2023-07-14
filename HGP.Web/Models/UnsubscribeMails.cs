using HGP.Common;
using HGP.Common.Database;
using HGP.Web.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models
{
    public class Unsubscribe : MongoObjectBase, ITextSearchSortable
    {
        public string PortalId { get; set; }
        public string PortalUserId { get; set; }
        public string PortalUserEmail { get; set; }

        public GlobalConstants.UnsubscribeTypes MailType { get; set; }

        public double? TextMatchScore { get; set; }
    }

    public class UnsubscribeHomeModel : PageModel
    {

        public Unsubscribe Unsubscribe { get; set; }
    }
}
