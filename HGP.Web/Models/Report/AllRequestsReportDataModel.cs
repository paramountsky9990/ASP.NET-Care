using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;

namespace HGP.Web.Models.Report
{
    public class AllRequestsReportLineItemModel
    {
        public string Location { get; set; }
        public string Status { get; set; }
        public string RequestorName { get; set; }
        public string RequestorPhone { get; set; }
        public string RequestorEmail { get; set; }
        public string RequestNum { get; set; }
        public int AssetCount { get; set; }
        public string HitNumbers { get; set; }
        public string AssetStatus { get; set; }
        public string ViewUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public string SerialNumber { get; set; }
        public string NetBookValues { get; set; }
        public DateTime RequestDate { get; set; }
        public string ReleaseDate { get; set; }
        public string Notes { get; set; }
        public string SiteResponse { get; set; }
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Country { get; set; }
        public string Attention { get; set; }
        public string ShippingNote { get; set; }


    }
    public class AllRequestsReportDataModel : ReportPageModel
    {
        public IList<AllRequestsReportLineItemModel> Requests { get; set; }

    }
}