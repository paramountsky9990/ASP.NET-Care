using System.Web;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Services;

namespace HGP.Web.Tests.Services
{
    internal class TestWorkContext : IWorkContext
    {
        public IPortalServices S { get; set; }
        public HttpContext HttpContext { get; set; }
        public string PortalTag { get; set; }
        public ISite CurrentSite { get; set; }
        public PortalUser CurrentUser { get; set; }
    }
}