
using System;
using System.Collections.Generic;

namespace HGP.Web.Models
{
    public class SiteSettingsDto
    {
        public bool IsSelfRegistrationOn { get; set; }
        public bool IsConfigured { get; set; }
        public bool IsOpen { get; set; }
        public string PortalTag { get; set; } // Unique per client!
        public string CompanyName { get; set; }
        public string SupportEmail { get; set; }
        public string Phone { get; set; }
        public string Street1 { get; set; }
        public string Street2 { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public int TimezoneOffset { get; set; }
        public int TimezoneId { get; set; }
        public int CurrencyId { get; set; }
        public int LanguageId { get; set; }
        public string GoogleUaNumber { get; set; }
        public int LogoFileId { get; set; }
        public DateTime? LastLogin { get; set; }
        public IList<string> Features { get; set; }
        public bool UseAssetOwnerForApproval { get; set; }
    }
}
