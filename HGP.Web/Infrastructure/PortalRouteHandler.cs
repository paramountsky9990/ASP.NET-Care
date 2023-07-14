#region Using

using System;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

#endregion

namespace HGP.Web.Infrastructure
{
    public class PortalRouteHandler : IRouteHandler
    {
        public PortalRouteHandler()
        {
            Logger = Log4NetLogger.GetLogger();
        }

        private static ILogger Logger { get; set; }

        /// <summary>
        ///     Custom handler for portal routes defined in database.
        ///     /admin is handled by a preset route in the route table
        /// </summary>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            Logger.Information("Entering PortalRouteHandler.GetHttpHandler");
            var tag = requestContext.RouteData.GetRequiredString("portaltag").ToLower();

            if (string.IsNullOrWhiteSpace(tag))
                return new MvcHandler(requestContext);

            var service = IoC.Container.GetInstance<ISiteService>();
            var siteModel = service.GetByPortalTag(tag);

            if (siteModel != null)
            {
                Logger.Information("Setting site [{0}] [{1}]", siteModel.SiteSettings.PortalTag, siteModel.SiteSettings.CompanyName);

                var wc = IoC.Container.GetInstance<IPortalServices>().WorkContext;
                wc.PortalTag = tag;
                wc.CurrentSite = siteModel;

                var userId = HttpContext.Current.User.Identity.GetUserId();

                if (userId != null)
                {
                    var manager = requestContext.HttpContext.GetOwinContext().GetUserManager<PortalUserService>();
                    var task = manager.FindByIdAsync(userId);

                    task.Wait();
                    wc.CurrentUser = task.Result;

                    if (wc.CurrentUser == null)
                    {
                        Logger.Information("Setting user - User not found [{0}]", userId);

                        var authManager = requestContext.HttpContext.GetOwinContext().Authentication;
                        authManager.SignOut();

                        HttpContext.Current.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);
                    }
                    else
                    {
                        service.SetLastLogin(siteModel.Id);
                        Logger.Information("Setting user [{0}]", wc.CurrentUser.Email);
                        manager.SetLastLogin(wc.CurrentUser.Id, DateTime.UtcNow);                       
                    }
                }
                else
                    Logger.Information("Setting user - User not found");
            }
            else
            {
                if ((tag.ToLower() == "content") || (tag.ToLower() == "bundles") || (tag.ToLower() == "glimpse"))
                    return new MvcHandler(requestContext);

                requestContext.RouteData.Values["controller"] = "Errors";
                requestContext.RouteData.Values["action"] = "NotFound";
                requestContext.HttpContext.Response.StatusCode = 404;
            }

            // Return the default MVC HTTP handler for the configured request
            return new MvcHandler(requestContext);
        }
    }
}