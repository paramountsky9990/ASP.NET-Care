using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using HGP.Common.Logging;
using HGP.Web.Infrastructure;
using HGP.Web.Services;

namespace HGP.Web.Controllers
{
    public class BaseController : Controller
    {
        private static ILogger Logger { get; set; }
        internal IPortalServices S { get; set; }

        public BaseController(IPortalServices portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("BaseController");

            this.S = portalServices;
        }
        
        protected override void Initialize(RequestContext requestContext)
        {
            Logger.Information("  We have a request");

            base.Initialize(requestContext);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (this.TempData["messages"] == null)
                this.TempData["messages"] = new List<AlertMessage>();

            base.OnActionExecuting(filterContext);
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            Logger.Error(filterContext.Exception, "Exception");
            base.OnException(filterContext);
        }

        public void DisplayMessage(string message, AlertSeverity severity)
        {
            ((List<AlertMessage>) TempData["messages"])?.Add(new AlertMessage() { Severity = severity, Message = message });
            TempData.Keep("messages");
        }

        public void DisplayErrorMessage(string message)
        {
            this.DisplayMessage(message, AlertSeverity.Error);
        }

        public void DisplayWarningMessage(string message)
        {
            this.DisplayMessage(message, AlertSeverity.Warning);
        }

        public void DisplaySuccessMessage(string message)
        {
            this.DisplayMessage(message, AlertSeverity.Success);
        }

        public void DisplayGeneralMessage(string message)
        {
            this.DisplayMessage(message, AlertSeverity.Message);
        }
    }
}
