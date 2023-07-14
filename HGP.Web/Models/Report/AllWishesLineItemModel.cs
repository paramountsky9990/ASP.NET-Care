using System;
using HGP.Common;

namespace HGP.Web.Models.Report
{
    public class AllWishesLineItemModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SearchCriteria { get; set; }
        public GlobalConstants.WishListStatusTypes Status { get; set; }
        public DateTime ExpireOn { get; set; }
    }
}