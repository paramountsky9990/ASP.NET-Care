using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AspNet.Identity.MongoDB;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Assets;
using Microsoft.AspNet.Identity;

namespace HGP.Web.Models
{
    // You can add profile data for the user by adding more properties to your PortalUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.

    public interface IApplicationUser
    {
        string Id { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string PortalId { get; set; }
        DateTime LastLogin { get; set; }

        new Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<PortalUser> manager);
    }

    public class PortalUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Address Address { get; set; }
        public string PortalId { get; set; }
        public DateTime LastLogin { get; set; }
        public IList<AdminAssetsHomeGridModel> RecentlyViewed { get; set; }
        public IList<RecentCategory> RecentCategories { get; set; }
        public IList<string> RecentSearches { get; set; }
        public string ApprovingManagerId { get; set; }
        public string ApprovingManagerName { get; set; }
        public string ApprovingManagerEmail { get; set; }
        public string ApprovingManagerPhone { get; set; }
        public PortalUser()
        {
            this.RecentlyViewed = new List<AdminAssetsHomeGridModel>();
            this.Address = new Address();
            this.RecentCategories = new List<RecentCategory>();
            this.RecentSearches = new List<string>();
        }
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<PortalUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }
}
