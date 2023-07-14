using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;

namespace HGP.Web.Models.Report
{
    public class AllWishesDataModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SearchCriteria { get; set; }
        public GlobalConstants.WishListStatusTypes Status { get; set; }
        public DateTime ExpireOn { get; set; }
    }
    public class AllWishesReportDataModel : ReportPageModel
    {
        public IList<AllWishesLineItemModel> Assets { get; set; }

    }
}