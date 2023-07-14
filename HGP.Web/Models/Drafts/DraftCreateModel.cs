using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HGP.Common;
using HGP.Web.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace HGP.Web.Models.Drafts
{
    public class DraftCreateModel : PageModel
    {
        public string Id { get; set; }
        public string PortalId { get; set; }
        public string HitNumber { get; set; }
        public string ClientIdNumber { get; set; }
        public string OwnerId { get; set; }
        [Required()]
        public GlobalConstants.AssetStatusTypes Status { get; set; }
        public GlobalConstants.DraftAssetStatusTypes DraftStatus { get; set; }
        public string Title { get; set; }
        [DisplayName("Visible")]
        public bool IsVisible { get; set; }
        [AllowHtml]
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        [DisplayName("Model Number")]
        public string ModelNumber { get; set; }
        [DisplayName("Serial Number")]
        public string SerialNumber { get; set; }
        public bool DisplayBookValue { get; set; }
        [DisplayName("Book Value")]
        public string BookValue { get; set; }
        public string Location { get; set; }
        [DisplayName("Service Status")]
        public string ServiceStatus { get; set; }
        public string Condition { get; set; }
        public string Category { get; set; }
        [DisplayName("Date Available")]
        public DateTime AvailForRedeploy { get; set; }
        [DisplayName("Expiration")]
        public DateTime AvailForSale { get; set; }
        public IList<MediaFileDto> Media { get; set; }

        public List<string> Categories { get; set; }
        public List<string> Locations { get; set; }

        public DateTime UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        [BsonIgnore]
        public bool SubmitForApproval { get; set; }


        public DraftCreateModel()
        {
            this.Media = new List<MediaFileDto>();
        }

    }
}