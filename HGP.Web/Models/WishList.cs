using HGP.Common.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;

namespace HGP.Web.Models
{
    public class WishList : MongoObjectBase, ITextSearchSortable
    {
        public string PortalId { get; set; }
        public string PortalUserId { get; set; }
        public GlobalConstants.WishListStatusTypes Status { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SearchCriteria { get; set; }
       // public bool IsExtended { get; set; }
        public DateTime ExpireOn { get; set; }
        public bool SendMail { get; set; }

        public double? TextMatchScore { get; set; }  
    }
}