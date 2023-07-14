using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Admin
{
    public class AdminHomeSiteGridModel
    {
        public string Id { get; set; }
        public bool IsOpen { get; set; }
        public string Name { get; set; }
        public string PortalTag { get; set; }
        public ContactInfo AccountExecutive { get; set; }
        public string CompanyName { get; set; }
        public int AssetCount { get; set; }
        public int PendingTransfersCount { get; set; }
        public int UsersCount { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}