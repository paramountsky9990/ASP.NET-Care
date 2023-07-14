using System;
using HGP.Common;

namespace HGP.Web.Models.Email
{
    public class AssetEmailModel
    {
        public string PortalId { get; set; }
        public string HitNumber { get; set; }
        public string OwnerId { get; set; }
        public GlobalConstants.AssetStatusTypes Status { get; set; }
        public string Title { get; set; }
        public bool IsVisible { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public int Quantity { get; set; }
        public string BookValue { get; set; }
        public string Location { get; set; }
        public string ServiceStatus { get; set; }
        public string Condition { get; set; }
        public string Category { get; set; }
        public string Catalog { get; set; }
        public string ImportBatchId { get; set; }
        public DateTime AvailForRedeploy { get; set; }
        public DateTime AvailForSale { get; set; }

        public string OwnerName { get; set; }
        public string OwnerPhone { get; set; }
        public string OwnerEmail { get; set; }

    }
}

