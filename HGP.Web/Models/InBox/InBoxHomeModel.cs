using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.InBox
{
    public class InBoxHomeModel : PageModel
    {
        public int InBoxCount { get; set; }

        public IList<InboxItem> RequestItems { get; set; }

        public IList<DraftAssetInboxItem> DraftAssetItems { get; set; }

        public InBoxHomeModel()
        {
            this.RequestItems = new List<InboxItem>();
            this.DraftAssetItems = new List<DraftAssetInboxItem>();
        }
    }
}