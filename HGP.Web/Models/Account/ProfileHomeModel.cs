using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.UI;
using HGP.Web.Security;

namespace HGP.Web.Models.Account
{
    public class EditContactInfoModel
    {
        public string UserId { get; set; }
        [Required]
        [Display(Name = "First name")]
        public string FirstName { get; set; }
        [Required]
        [Display(Name = "Last name")]
        public string LastName { get; set; }
        [Required]
        [EmailAddress]
        [FilterDomains]
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone")]
        public string PhoneNumber { get; set; }
    }

    public class EditAddressModel
    {
        public string UserId { get; set; }
        public Address Address { get; set; }
    }

    public class EditPasswordModel
    {
        public string UserId { get; set; }
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 2)]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string Password { get; set; }
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 2)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class EditManagerModel
    {
        public string UserId { get; set; }
        [Display(Name = "Manager")]
        public string ApprovingManagerName { get; set; }
        [Display(Name = "Email")]
        public string ApprovingManagerEmail { get; set; }
        [Display(Name = "Phone")]
        public string ApprovingManagerPhone { get; set; }
    }

    public class ProfileHomeModel : PageModel
    {
        public EditContactInfoModel ContactInfoModel { get; set; }
        public EditAddressModel AddressModel { get; set; }
        public EditPasswordModel PasswordModel { get; set; }
        public EditManagerModel ManagerModel { get; set; }

        public List<string> Roles { get; set; }
        public bool LockoutEnabled { get; set; }

    }
}