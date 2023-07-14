using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Admin
{
    public class AdminAssetsHomeGridModel
    {
        public string Id { get; set; }
        public string HitNumber { get; set; }
        public string Title { get; set; }
        public string ClientIdNumber { get; set; }
        public string Status { get; set; }
        public string OwnerId { get; set; }
        public string OwnerName { get; set; }
        public bool IsVisible { get; set; }
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public string SerialNumber { get; set; }
        public string BookValue { get; set; }
        public string Location { get; set; }
        public string ServiceStatus { get; set; }
        public string Condition { get; set; }
        public string Category { get; set; }
        public string Catalog { get; set; }
        public string Selected { get; set; }
        public DateTime AvailForRedeploy { get; set; }
        public DateTime AvailForSale { get; set; }
        public IList<AdminAssetsHomeGridMediaModel> Media { get; set; }
        public int Images { get; set; }

        public AdminAssetsHomeGridModel()
        {
            Selected = "0";
        }
    }
}