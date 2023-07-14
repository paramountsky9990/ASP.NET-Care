using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Web.Models.Assets;

namespace HGP.Web.Models.Admin
{
    public class PortalUserDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public Address Address { get; set; }
        public string PortalId { get; set; }
        public DateTime LastLogin { get; set; }
        public IList<string> Roles { get; set; }
        public string ApprovingManagerId { get; set; }
        public string ApprovingManagerName { get; set; }
        public string ApprovingManagerEmail { get; set; }
        public string ApprovingManagerPhone { get; set; }
    }
}