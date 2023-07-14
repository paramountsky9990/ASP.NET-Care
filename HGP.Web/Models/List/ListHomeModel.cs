#region Using

using System.Collections.Generic;
using HGP.Web.Models.Assets;
using Newtonsoft.Json;

#endregion

namespace HGP.Web.Models.List
{
    public class ListHomeModel : PageModel
    {
        public ListHomeModel()
        {
            SiteSettings = new SiteSettings();
        }

        public IEnumerable<ListHomeGridModel> Assets { get; set; }

        [JsonIgnore] // Do not serialize Categories, knockout is not used for binding
        public List<CategoryListModel> Categories { get; set; }
        [JsonIgnore] // Do not serialize Locations, knockout is not used for binding
        public List<LocationListModel> Locations { get; set; }

        public int PageNumber { get; set; }
        public string SearchText { get; set; }
        public int ResultCount { get; set; }

        [JsonIgnore]
        public string JsonData { get; set; }

        public string Category { get; set; } // used to display category name at top of listing
        public string CategoryUri { get; set; } // used to filter listing page
        public string Location { get; set; }
        public string LocationUri { get; set; } // used to filter listing page
    }
}