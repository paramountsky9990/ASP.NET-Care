using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace HGP.Web.Models
{
    public class Address
    {
        [Required(ErrorMessage = "A street address is required")]
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        [Required(ErrorMessage = "A city is required")]
        public string City { get; set; }
        [Required(ErrorMessage = "A state is required")]
        public string State { get; set; }
        [Required(ErrorMessage = "A zip code is required")]
        public string Zip { get; set; }
        public string Country { get; set; }
        public string Attention { get; set; }
        public string Notes { get; set; }
    }
}