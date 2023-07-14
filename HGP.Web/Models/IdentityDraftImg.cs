using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models
{
    public class IdentityDraftImg
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Path_1 { get; set; }
        public string DraftId { get; set; }
    }
}