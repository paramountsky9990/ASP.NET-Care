using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.WishLists
{
    public class WishListHomeModel : PageModel
    {
        public WishListHomeModel()
        {
            this.SiteSettings = new SiteSettings();
        }

        public List<WishListDetails> WishListDetails { get; set; }

        [JsonIgnore]
        public string JsonData { get; set; }
    }

    public class WishListDetails
    {
        public WishList WishList { get; set; }
        public bool IsEmailSent { get; set; }
    }

}