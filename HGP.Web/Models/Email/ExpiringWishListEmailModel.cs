using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Email
{
    public class ExpiringWishListEmailModel: EmailModel
    {
        public List<ExtendWishList> ExtendWishLists { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }

        internal ExpiringWishListEmailModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
        {

        }
    }

    public class ExtendWishList
    {
        public WishList WishList { get; set; }
        public string ExtendURL { get; set; }
    }
}