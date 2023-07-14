using System;
using System.Collections.Generic;
using HGP.Common;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Requests;
using MongoDB.Bson.IO;
using Quartz.Util;

namespace HGP.Web.Models
{
    public class AssetRequestEmailDto
    {
        /// <summary>
        /// AssetId
        /// </summary>
        public string Id { get; set; }
        public GlobalConstants.RequestStatusTypes Status { get; set; }
        public string HitNumber { get; set; }
        public string ClientIdNumber { get; set; }
        public string Title { get; set; }
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public string BookValue { get; set; }
        public string SerialNumber { get; set; }
        public bool DisplayBookValue { get; set; }
        public int Quantity { get; set; }
        public string LocationName { get; set; }
        public string LocationOwnerName { get; set; }
        public string LocationOwnerEmail { get; set; }
        public string LocationOwnerPhone { get; set; }
        public Address AssetAddress { get; set; }
        public string Category { get; set; }
        public string Catalog { get; set; }
        public string OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
        public string OwnerPhone { get; set; }
        public IList<AdminAssetsHomeGridMediaModel> Media { get; set; }
        public string TaskComment { get; set; }
        public string TaskType { get; set; }
        public int TaskCurrentStep { get; set; }
        public IList<TaskStep> TaskSteps { get; set; }
        public string CustomData { get; set; }
        public List<KeyValuePair<string, string>> CustomDataPairs { get; set; }

        public bool HasCustomData
        {
            get { return !CustomData.IsNullOrWhiteSpace(); }
        }
        public bool HasClientIdNumber
        {
            get { return !ClientIdNumber.IsNullOrWhiteSpace(); }
        }
        public AssetRequestEmailDto()
        {
            this.CustomDataPairs = new List<KeyValuePair<string, string>>();
        }
        public string FormattedBookValue
        {
            get { return string.Format("{0:C0}", decimal.Parse(this.BookValue)); }
        }
    }
}