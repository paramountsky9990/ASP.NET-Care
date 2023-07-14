using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HGP.Web.Models.User
{
    public class UserCreateModel : PageModel
    {
        [Required]
        [EmailAddress]
        [Remote("DoesUserNameExist", "User", HttpMethod = "POST", ErrorMessage = "Email address already exists.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone")]
        public string Phone { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        public string Role { get; set; }

        public string SiteId { get; set; }
        public bool SendWelcomeMessage { get; set; }

        public UserCreateModel()
        {
            this.SendWelcomeMessage = true;
        }
    }
}
