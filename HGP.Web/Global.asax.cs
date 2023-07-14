using System;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using HGP.Common;
using HGP.Common.Logging;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Services;
using HGP.Web.Utilities;
using Microsoft.Owin.BuilderProperties;
using Owin;
using StructureMap.Web.Pipeline;

namespace HGP.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static ILogger Logger { get; set; }

        JobScheduler JobScheduler;

        public MvcApplication()
        {
            BeginRequest += MyBeginRequest;
            Error += OnError;
        }

        private void OnError(object sender, EventArgs eventArgs)
        {
            // Get the error details
            var lastErrorWrapper = Server.GetLastError();

            Exception lastError = lastErrorWrapper;
            if (lastErrorWrapper.InnerException != null)
                lastError = lastErrorWrapper.InnerException;

            string lastErrorTypeName = lastError.GetType().ToString();
            string lastErrorMessage = lastError.Message;
            string lastErrorStackTrace = lastError.StackTrace;

            Logger.Error("Unhandled exception {0}, {1} {2}", lastErrorTypeName, lastErrorMessage, lastErrorStackTrace);
        }

        private void MyBeginRequest(object sender, EventArgs e)
        {
            RouteData routeData = RouteTable.Routes.GetRouteData(new HttpContextWrapper(HttpContext.Current));
            if (routeData == null)
                return;
            string s = string.Join("; ", routeData.Values.Select(x => x.Key + "=" + x.Value).ToArray());

            var currentContext = new HttpContextWrapper(HttpContext.Current);
            Logger.Information("*** New request [{0}] - Url: [{1}]", s, currentContext.Request.RawUrl);
        }


        protected void Application_Start()
        {
            var log4NetPath = Server.MapPath("~/log4net.config");
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(log4NetPath));
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("*****************************************************");
            Logger.Information("****** HGP.Web Application_Start called ******");
            Logger.Information("*****************************************************");

            MapperConfig.Configure();
            EnsureIndexes.Exist();
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            GlobalFilters.Filters.Add(new HandleErrorAttribute());
            ModelBinders.Binders.Add(typeof(string), new TrimModelBinder()); // http://stackoverflow.com/questions/1718501/asp-net-mvc-best-way-to-trim-strings-after-data-entry-should-i-create-a-custo

            System.Diagnostics.Debug.WriteLine(IoC.Container.WhatDoIHave());
            //IoC.Container.AssertConfigurationIsValid();

            InitDatabase.CheckIndexes();
            InitDatabase.GenData();

            var enableJobs = bool.Parse(WebConfigurationManager.AppSettings["EnableScheduledJobs"]);
            if (enableJobs)
            {
                JobScheduler = new JobScheduler();
                JobScheduler.StartAsync();
            }

            IoC.Container.GetInstance<IActivityLogService>().LogActivity(GlobalConstants.ActivityTypes.WebAppStarted);
        }

        protected void Application_EndRequest()
        {
            // Clear out SM's objects
            HttpContextLifecycle.DisposeAndClearAll();

            //if (Context.Response.StatusCode == 404)
            //{
            //    Response.Clear();

            //    var rd = new RouteData();
            //    rd.DataTokens["area"] = "AreaName"; // In case controller is in another area
            //    rd.Values["controller"] = "Errors";
            //    rd.Values["action"] = "NotFound";

            //    IController c = new ErrorsController();
            //    c.Execute(new RequestContext(new HttpContextWrapper(Context), rd));
            //}
        }
    }

    static class AppBuilderExtensions
    {
        public static void OnDisposing(this IAppBuilder app, Action cleanup)
        {
            var properties = new AppProperties(app.Properties);
            var token = properties.OnAppDisposing;
            if (token != CancellationToken.None)
            {
                token.Register(cleanup);
            }
        }
    }

    public class MyStartup
    {
        public void Configuration(IAppBuilder app)
        {
            app.OnDisposing(() =>
            {
                IoC.Container.GetInstance<IActivityLogService>().LogActivity(GlobalConstants.ActivityTypes.WebAppStopped);
            });
        }
    }
}
