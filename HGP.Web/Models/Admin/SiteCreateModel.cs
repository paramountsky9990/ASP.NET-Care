using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using AutoMapper;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Services;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
    
namespace HGP.Web.Models.Admin
{
    public class SiteCreateModel: AdminPageModel
    {
        public bool IsOpen { get; set; }
        [Required]
        public string CompanyName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SupportEmail { get; set; }
        public string Phone { get; set; }
        [Required]
        public string LocationName { get; set; }
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string AddressNotes { get; set; }
        public int TimezoneOffset { get; set; }
        public int TimezoneId { get; set; }
        public int CurrencyId { get; set; }
        public int LanguageId { get; set; }
        public string GoogleUaNumber { get; set; }
        public string Password { get; set; }
        public string PasswordMessage { get; set; }
        public int LogoFileId { get; set; }

        [Required]
        [DisplayName("Portal Tag")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Invalid portal tag")]
        public string PortalTag { get; set; }
        public string EmailRegex { get; set; }
        public int? AssetExpirationDays { get; set; }
        public IList<ContactInfo> AEs { get; set; }
        [Required]
        public string CurrentAe { get; set; }
            
        [JsonIgnore]
        public string JsonData { get; set; }

    }
}