using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;
using HGP.Common.Database;
using MongoDB.Bson.Serialization.Attributes;

namespace HGP.Web.Models.InBox
{
    public class DraftAssetInboxItem : MongoObjectBase
    {
        public string PortalId { get; set; }
        public GlobalConstants.InboxStatusTypes Status { get; set; }
        public GlobalConstants.InboxItemTypes Type { get; set; }
        public string DraftAssetHitNumber { get; set; }
        public string Notes { get; set; }
        [BsonIgnore]
        public DraftAsset DraftAsset { get; set; }
    }
}