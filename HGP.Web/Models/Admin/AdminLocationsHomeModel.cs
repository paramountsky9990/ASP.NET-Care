using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace HGP.Web.Models.Admin
{
    public class AdminLocationsHomeModel : AdminPageModel
    {
        public string SiteId { get; set; }
        public string PortalTag { get; set; }
        public IList<AdminLocationsHomeGridModel> Locations { get; set; }

        [JsonIgnore]
        public string JsonData { get; set; }
    }

    public class AdminLocationsHomeGridModel
    {
        public string Name { get; set; }
        public Address Address { get; set; }
        public string OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string OwnerPhone { get; set; }
        public string OwnerEmail { get; set; }
        public int VisibleAssetCount { get; set; }
        public int HiddenAssetCount { get; set; }
    }
}