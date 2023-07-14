using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace HGP.Web.Controllers
{
    public class HealthCheckController : Controller
    {
        public HealthCheckController()
        {
        }

        [HttpGet]
        public ActionResult Index()
        {
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}