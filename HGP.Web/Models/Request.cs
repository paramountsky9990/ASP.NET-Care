using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;
using HGP.Common.Database;
using HGP.Web.Database;
using HGP.Web.Models.Requests;
using HGP.Web.Services;

namespace HGP.Web.Models
{
    public class Request : MongoObjectBase
    {
        public GlobalConstants.RequestStatusTypes Status { get; set; }
        public string PortalId { get; set; }
        public string RequestorId { get; set; }
        public string RequestorName { get; set; }
        public string RequestorPhone { get; set; }
        public string RequestorEmail { get; set; }
        public string ApprovingManagerId { get; set; }
        public string ApprovingManagerName { get; set; }
        public string ApprovingManagerEmail { get; set; }
        public string ApprovingManagerPhone { get; set; }
        public string RequestNum { get; set; }
        public int AssetCount { get; set; }
        public IList<AssetRequestDetail> AssetRequests { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime ClosedDate { get; set; }
        public bool IsShipToAddressValid { get; set; }
        public Address ShipToAddress { get; set; }
        public string Notes { get; set; }
        public Request()
        {
            this.AssetRequests = new List<AssetRequestDetail>();
            this.ShipToAddress = new Address();
            this.IsShipToAddressValid = false;
        }
    }
}