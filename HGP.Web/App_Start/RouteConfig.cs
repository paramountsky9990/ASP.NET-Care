using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using HGP.Web.Infrastructure;

namespace HGP.Web
{
    public class RouteConfig
    {
        //http://stackoverflow.com/questions/18312703/supporting-multi-tenant-routes-in-mvc

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.RouteExistingFiles = false;
            routes.LowercaseUrls = true;
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(name: "HealthCheck", url: "healthcheck/{action}", defaults: new { controller = "HealthCheck", action = "Index", id = UrlParameter.Optional });


            routes.MapRoute(name: "Account", url: "account/{action}/{id}", defaults: new { controller = "Account", action = "Index", id = UrlParameter.Optional });


            var route = new Route("admin/{controller}/{action}/{id}",
                new RouteValueDictionary(new
                {
                    controller = "AdminHome",
                    action = "Index",
                    id = UrlParameter.Optional
                }), new AdminPortalRouteHandler());
            routes.Add("AdminPortalRoute", route);

            route = new Route("admin/adminlocations/{action}/{id}/{locationName}",
                new RouteValueDictionary(new
                {
                    controller = "AdminLocations",
                    action = "Index",
                    id = UrlParameter.Optional,
                    locationName = UrlParameter.Optional
                }), new AdminPortalRouteHandler());
            routes.Add("AdminLocationPortalRoute", route);

            route = new Route("{portaltag}/{controller}/{action}/{id}",
                new RouteValueDictionary(new
                {
                    controller = "Portal",
                    action = "Index",
                    id = UrlParameter.Optional
                }), new PortalRouteHandler());
            routes.Add("PortalRoute", route);

            route = new Route("{portaltag}/list/{action}/{searchText}",
                new RouteValueDictionary(new
                {
                    controller = "List",
                    action = "Search",
                    searchText = UrlParameter.Optional
                }), new PortalRouteHandler());
            routes.Add("ListRoute", route);

            routes.MapRoute(
                name: "GuestRoute",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Account", action = "GuestLogin", id = UrlParameter.Optional }
            );
            

    
           // route = new Route("{portaltag}/{controller}/{action}/{id}",
           //     new RouteValueDictionary(new
           //     {
           //         controller = "PortalController",
           //         action = "Index",
           //         id = UrlParameter.Optional
           //     }), new PortalRouteHandler());
           // routes.Add("PortalRoute", route); 
            
           // routes.MapRoute(
           //    "Default", // Route name
           //     "{controller}/{action}/{id}",
           //    new { controller = "HomeController", action = "Index" } // Parameter defaults,
           //);

           // routes.MapRoute(
           //    "Default0", // Route name
           //    "test1/{action}", // URL with parameters
           //    new { controller = "PortalController", action = "Index" } // Parameter defaults,
           //);

           // routes.MapRoute(
           //    "Default", // Route name
           //    "{portaltag}/{controller}/{action}", // URL with parameters
           //    new { portaltag = "Default", controller = "PortalController", action = "Index" } // Parameter defaults,
           //);

           // routes.MapRoute(
           //    "Default2", // Route name
           //    "{portaltag}/{controller}/{action}/{id}", // URL with parameters
           //    new { portaltag = "Default", controller = "PortalController", action = "Index", id = UrlParameter.Optional } // Parameter defaults,
           //);

        }
    }
}
