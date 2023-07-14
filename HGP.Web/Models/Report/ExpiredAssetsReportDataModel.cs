using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;

namespace HGP.Web.Models.Report
{
    public class ExpiredAssetReportLineItemModel
    {
        public string HitNumber { get; set; }
        public string ClientIdNumber { get; set; }
        public GlobalConstants.AssetStatusTypes Status { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public string BookValue { get; set; }
        public string Location { get; set; }
        public string Category { get; set; }
        public DateTime AvailForRedeploy { get; set; }
        public DateTime AvailForSale { get; set; }
        public IList<MediaFileDto> Media { get; set; }
    }
    public class ExpiredAssetsReportDataModel : ReportPageModel
    {
        public IList<ExpiredAssetReportLineItemModel> Assets { get; set; }

    }
}