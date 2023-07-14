using HGP.Common;
using HGP.Common.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models
{
    public class MatchedAsset : MongoObjectBase
    {
        public string WishLIstID { get; set; }
        public string AssetID { get; set; }
        public bool IsEmailSent { get; set; }
        public GlobalConstants.MatchedAssetStatusTypes Status { get; set; }
    }
}