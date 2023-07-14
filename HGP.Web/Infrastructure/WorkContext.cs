#region Using

using System;
using System.Diagnostics.Contracts;
using System.Web;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Models;
using HGP.Web.Services;

#endregion

namespace HGP.Web.Infrastructure
{
    public class WorkContext : IWorkContext
    {
        private string currentCulture;
        private Lazy<ISite> currentSite;
        private IPortalServices portalServices;

        public WorkContext(HttpContext context)
        {
            Logger = Log4NetLogger.GetLogger();

            HttpContext = context;

            currentSite = Lazy.From(() =>
            {
                {
                    Logger.Information("Loading by hostname: {0}", PortalTag);
                    var site = S.SiteService.GetByPortalTag(PortalTag);

                    if (site == null)
                        Logger.Information("No site loaded");
                    else
                        Logger.Information("Loaded site [{0}] [{1}]", site.SiteSettings.PortalTag,
                            site.SiteSettings.CompanyName);

                    return site;
                }
            });
        }

        private static ILogger Logger { get; set; }

        public IPortalServices S
        {
            get
            {
                if (portalServices == null)
                    portalServices = IoC.Container.GetInstance<IPortalServices>();
                Contract.Assert(portalServices != null);
                return portalServices;
            }

            set { portalServices = value; }
        }

        public HttpContext HttpContext { get; set; }

        public string PortalTag { get; set; }

        public ISite CurrentSite
        {
            get { return currentSite.Value; }
            set
            {
                Logger.Information("Set site [{0}] [{1}]", value.SiteSettings.CompanyName, value.SiteSettings.PortalTag);
                currentSite = new Lazy<ISite>(() => value);
            }
        }

        public PortalUser CurrentUser { get; set; }
    }
}