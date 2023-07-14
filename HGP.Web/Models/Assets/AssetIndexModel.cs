using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models.List;
using HGP.Web.Services;
using Newtonsoft.Json;

namespace HGP.Web.Models.Assets
{
    public class AssetIndexModel : PageModel
    {
        public string PortalId { get; set; }
        public string HitNumber { get; set; }
        public string ClientIdNumber { get; set; }
        public string OwnerId { get; set; }
        public string Title { get; set; }
        public bool IsVisible { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public string SerialNumber { get; set; }
        public int Quantity { get; set; }
        public string BookValue { get; set; }
        public bool DisplayBookValue { get; set; }
        public string Location { get; set; }
        public string OwnerName { get; set; }
        public string OwnerPhone { get; set; }
        public string OwnerEmail { get; set; }
        public string ServiceStatus { get; set; }
        public string Condition { get; set; }
        public string Category { get; set; }
        public string Catalog { get; set; }
        public string ImportBatchId { get; set; }
        public DateTime AvailForRedeploy { get; set; }
        public DateTime AvailForSale { get; set; }
        public IList<MediaFileDto> Media { get; set; }
        public string CustomData { get; set; }
        [JsonIgnore]
        public string JsonData { get; set; }
        public Double MinutesRemaining { get; set; }
        public int RequestCount { get; set; }
    }
}