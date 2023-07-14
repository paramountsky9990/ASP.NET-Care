using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Admin
{
    public class LocationCreateModel : AdminPageModel
    {
        public string SiteId { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
        public string OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string OwnerPhone { get; set; }
        public string OwnerEmail { get; set; }
    }
}