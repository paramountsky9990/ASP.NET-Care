using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;
using HGP.Common.Database;
using HGP.Web.Database;
using MongoDB.Bson.Serialization.Attributes;
using Quartz.Util;

namespace HGP.Web.Models
{


    public class DraftAsset : MongoObjectBase, ITextSearchSortable
    {
        public string PortalId { get; set; }
        public string HitNumber { get; set; }
        public string ClientIdNumber { get; set; }
        public string OwnerId { get; set; }
        public string OwnerFirstName { get; set; }
        public string OwnerLastName { get; set; }
        public string OwnerPhone { get; set; }
        public string OwnerEmail { get; set; }
        public GlobalConstants.AssetStatusTypes Status { get; set; }
        public GlobalConstants.DraftAssetStatusTypes DraftStatus { get; set; }
        public string Title { get; set; }
        public bool IsVisible { get; set; }
        public string Description { get; set; }
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public string SerialNumber { get; set; }
        /// <summary>
        /// Deprecated!
        /// </summary>
        [Obsolete("Quantity is deprecated, value is always 1.")]
        public int Quantity { get; set; }
        public bool DisplayBookValue { get; set; }
        public string BookValue { get; set; }
        public string Location { get; set; }
        public string ServiceStatus { get; set; }
        public string Condition { get; set; }
        public string Category { get; set; }
        public string Catalog { get; set; }
        public string ImportBatchId { get; set; }
        public DateTime AvailForRedeploy { get; set; }
        public DateTime AvailForSale { get; set; }
        public IList<MediaFileDto> Media { get; set; }
        public string CustomData { get; set; }
        public string Notes { get; set; }
        public DateTime ApprovedDate { get; set; } 
        public DraftAsset()
        {
            this.Media = new List<MediaFileDto>();
            this.DisplayBookValue = true;
        }

        public double? TextMatchScore { get; set; }

        [BsonIgnore]
        public bool HasCustomData
        {
            get { return !CustomData.IsNullOrWhiteSpace(); }
        }
        [BsonIgnore]
        public bool HasClientIdNumber
        {
            get { return !ClientIdNumber.IsNullOrWhiteSpace(); }
        }
        [BsonIgnore]
        public string FormattedBookValue
        {
            get { return string.Format("{0:C0}", decimal.Parse(this.BookValue)); }
        }
    }
}