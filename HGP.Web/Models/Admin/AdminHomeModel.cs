using System.Collections.Generic;

namespace HGP.Web.Models.Admin
{
    public class AdminHomeModel : AdminPageModel
    {
        public IList<AdminHomeSiteGridModel> Sites { get; set; }

        public string JsonData { get; set; }
    }
}