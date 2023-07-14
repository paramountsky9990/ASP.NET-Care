using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HGP.Web.Security;

namespace HGP.Web.Models.Admin
{
    public class AdminUserCreateModel : AdminPageModel
    {
        [Required]
        [EmailAddress]
        [Remote("DoesUserNameExist", "AdminUsers", HttpMethod = "POST", ErrorMessage = "Email address already exists.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        public string Role { get; set; }

        public string SiteId { get; set; }
        public bool SendWelcomeMessage { get; set; }

        public AdminUserCreateModel()
        {
            this.SendWelcomeMessage = true;
        }
    }
}
