#region Using

using System.Collections.Generic;
using HGP.Web.Models.Assets;
using HGP.Web.Models.List;
using Newtonsoft.Json;

#endregion

namespace HGP.Web.Models
{
    public class PortalHomeModel : PageModel
    {
        public PortalHomeModel()
        {
            SiteSettings = new SiteSettings();
        }

        public IEnumerable<RecentCategory> RecentCategories { get; set; }
        public IEnumerable<string> RecentSearches { get; set; }
        public IEnumerable<RecentCategory> AllManufacturers { get; set; }
        public IEnumerable<RecentCategory> AllCategories { get; set; }
        public IEnumerable<ListHomeGridModel> RecentlyAddedAssets { get; set; }
        public IEnumerable<ListHomeGridModel> RecentlyViewedAssets { get; set; }

        [JsonIgnore]
        public string JsonData { get; set; }
    }
}