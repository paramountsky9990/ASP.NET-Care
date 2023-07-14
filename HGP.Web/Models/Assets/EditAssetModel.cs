using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using HGP.Common;

namespace HGP.Web.Models.Assets
{
    public class EditAssetModel : PageModel
    {
        public string AsssetId { get; set; }
        public string PortalId { get; set; }
        public string HitNumber { get; set; }
        public string Title { get; set; }
        [DisplayName("Visible")]
        public bool IsVisible { get; set; }
        [AllowHtml]
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        [DisplayName("Model Number")]
        public string ModelNumber { get; set; }
        [DisplayName("Serial Number")]
        public string SerialNumber { get; set; }
        public int Quantity { get; set; }
        [DisplayName("Book Value")]
        public string BookValue { get; set; }
        public string Location { get; set; }
        [DisplayName("Service Status")]
        public string ServiceStatus { get; set; }
        public string Condition { get; set; }
        public string Category { get; set; }
        public string Catalog { get; set; }
        [DisplayName("Date Available")]
        public DateTime AvailForRedeploy { get; set; }
        [DisplayName("Expiration")]
        public DateTime AvailForSale { get; set; }
    }
}