using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace HGP.Web.Models.Admin
{
    public class AdminUsersHomeModel : AdminPageModel
    {
        public string SiteId { get; set; }
        public string PortalTag { get; set; }
        public IList<PortalUserDto> Users { get; set; }

        [JsonIgnore]
        public string JsonData { get; set; }
    }
}