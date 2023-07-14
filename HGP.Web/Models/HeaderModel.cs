using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Web.Models.Assets;
using Newtonsoft.Json;

namespace HGP.Web.Models
{
    public class HeaderModel
    {
        public Request Request { get; set; }
        public string PortalTag { get; set; }
        public int InBoxCount { get; set; }
        public int TransfersCount { get; set; }
        public int PendingDraftAssetsCount { get; set; }
        public int PendingTransfersCount { get; set; }
        public int ClosedTransfersCount { get; set; }
        public IList<Category> Categories { get; set; }
        public string SearchText { get; set; }
        public int ResultCount { get; set; }
        public string ReferringPage { get; set; }

        [JsonIgnore]
        public string JsonData { get; set; }

        public HeaderModel()
        {
            this.InBoxCount = 0;
            this.TransfersCount = 0;
        }
    }
}