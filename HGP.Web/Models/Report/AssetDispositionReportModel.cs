using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Ajax.Utilities;

namespace HGP.Web.Models.Report
{
    public class AssetDispositionLineItem
    {
        // Columns - HitNumber, Status, Title, BookValue, Location, AvailForRedeploy, AvailForSale
        public string HitNumber { get; set; }
        public string ClientIdNumber { get; set; }
        public string Status { get; set; }
        public string Title { get; set; }
        public string BookValue { get; set; }
        public string Location { get; set; }
        public string AvailForRedeploy { get; set; }
        public string AvailForSale { get; set; }
        public string RequestedDate { get; set; } 
    }
    public class AssetDispositionDataModel : ReportPageModel
    {
        public IList<AssetDispositionLineItem> Assets { get; set; }
    }

    public class AssetDispositionReportModel : ReportPageModel
    {

    }
}