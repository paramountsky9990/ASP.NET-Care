using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Web.Models.User;
using Newtonsoft.Json;

namespace HGP.Web.Models.Admin
{
    public class AdminUsersPortalModel : AdminPageModel
    {
        public string SiteId { get; set; }
        public string PortalTag { get; set; }
        public IList<PortalUserDto> Users { get; set; }

        public UserCreateModel NewUserModel { get; set; }

        [JsonIgnore]
        public string JsonData { get; set; }
    }
}