using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Web.Models;
using HGP.Web.Models.Email;

public class ExpiringAssetsEmailModel : EmailModel
    {
        public List<AssetsExpiring> ExpiringAssets { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }
        public string CompanyName { get; set; }


    internal ExpiringAssetsEmailModel(HttpContext context, SiteSettings siteSettings) : base(context, siteSettings)
    {

    }

    public class AssetsExpiring
    {
        public Asset Asset { get; set; }
        public string PrimaryImageURL { get; set; }
        public string AssetURL { get; set; }
    }

}



