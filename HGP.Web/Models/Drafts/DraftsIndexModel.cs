using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Drafts
{
    public class DraftsIndexModel : PageModel
    {
        public List<DraftAsset> DraftAssets { get; set; }
        public string CurrentUserId { get; set; }
    }
}