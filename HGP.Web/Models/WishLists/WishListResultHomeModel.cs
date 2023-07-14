using HGP.Web.Models.List;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.WishLists
{
    public class WishListResultHomeModel : PageModel
    {
        public WishListResultHomeModel()
        {
            this.SiteSettings = new SiteSettings();
        }

        public WishList WishList { get; set; }
        public List<ListHomeGridModel> Assets { get; set; }


        [JsonIgnore]
        public string JsonData { get; set; }

    }
}