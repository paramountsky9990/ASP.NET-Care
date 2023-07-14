using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class WishListMatchedAssetModel:EmailModel
    {
        public List<AssetsUploaded> AssetsUploaded { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }

        public string WishListSearchCriteria { get; set; }

        public string MatchedWishListURL { get; set; }

        internal WishListMatchedAssetModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {

        }
    }
}