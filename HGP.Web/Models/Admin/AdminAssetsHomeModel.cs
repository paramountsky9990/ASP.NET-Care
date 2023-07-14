using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Admin
{
    public class AdminAssetsHomeModel : AdminPageModel
    {
        public IEnumerable<AdminAssetsHomeGridModel> Assets { get; set; }

        public AdminAssetsHomeModel()
        {
            this.SiteSettings = new SiteSettings();
        }
    }
}