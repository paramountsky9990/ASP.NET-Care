using System.Web;
using HGP.Web.Models;
using HGP.Web.Services;

namespace HGP.Web.Infrastructure
{
    public interface IWorkContext
    {
        IPortalServices S { get; set; }
        HttpContext HttpContext { get; set; }
        string PortalTag { get; set; }
        ISite CurrentSite { get; set;  }
        PortalUser CurrentUser { get; set; }
    }
}
