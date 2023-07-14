#region Using

using System.Collections.Generic;

#endregion

namespace HGP.Web.Models
{
    public class PageModel
    {
        protected PageModel()
        {
            MenuItems = new List<MenuItem>();
        }

        public HeaderModel HeaderModel { get; set; }
        public SiteSettings SiteSettings { get; set; }
        public List<MenuItem> MenuItems { get; private set; }
    }

    public class ReportPageModel : PageModel
    {
        public object Data { get; set; }
    }

    public class AdminPageModel : PageModel
    {
        public string CurrentDatabase { get; set; }
    }
}

