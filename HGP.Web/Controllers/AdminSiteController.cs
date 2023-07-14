using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Models.Admin;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;

namespace HGP.Web.Controllers
{
    public class AdminSiteControllerMappingProfile : Profile
    {
        public AdminSiteControllerMappingProfile()
        {
            CreateMap<SiteCreateModel, SiteSettings>();
        }
    }

    [Authorize(Roles = "SuperAdmin")]
    public class AdminSiteController : BaseController
    {
         public static ILogger Logger { get; set; }

         public AdminSiteController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("AdminSiteController");            
        }
    
    // GET: AdminPortal
        public ActionResult Index()
        {
            return View();
        }

        // GET: AdminPortal/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AdminPortal/Create
        [AcceptVerbs("GET")]
        public ActionResult Create()
        {
            var model = this.S.SiteService.BuildSiteCreateModel();
            
            return View("~/views/adminsite/create.cshtml", model);
        }

        // POST: AdminPortal/Create
        [AcceptVerbs("POST")]
        public ActionResult Create(SiteCreateModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var site = new Site();
                    Mapper.Map<SiteCreateModel, SiteSettings>(model, site.SiteSettings);
                    var userManager = IoC.Container.GetInstance<PortalUserService>();
                    if (!string.IsNullOrEmpty(model.CurrentAe))
                    {
                        var ae = userManager.FindByEmail(model.CurrentAe);
                        if (ae != null)
                            site.AccountExecutive = new ContactInfo()
                            {
                                Email = ae.Email,
                                FirstName = ae.FirstName,
                                LastName = ae.LastName,
                                PhoneNumber = ae.PhoneNumber
                            };                        
                    }

                    site.SiteSettings.IsAdminPortal = false;
                    this.S.SiteService.CreateBucket(site);
                    this.S.SiteService.Save(site);

                    var locationsUrl = Url.RouteUrl("AdminPortalRoute", new { action = "index", controller = "AdminUsers", portalTag = model.PortalTag });
                    if ((List<AlertMessage>)TempData["messages"] != null)
                        ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Site successfully created. The next step is to add some <a href='" + Server.HtmlEncode(locationsUrl) + "'>locations.</a>" });
                    
                    return RedirectToRoute("AdminPortalRoute", new { controller = "AdminHome", action = "Index"});
                }

                return View("~/views/adminsite/create.cshtml", model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // GET: AdminPortal/Edit/5
        public ActionResult Edit(string id)
        {
            var site = this.S.SiteService.GetByPortalTag(id);
            var model = this.S.SiteService.BuildAdminSiteSettingsModel(site.Id);
            return View(model);
        }

        // POST: AdminPortal/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        public ActionResult Delete(string siteId)
        {
            this.S.SiteService.Delete(siteId);

            return Json(new { success = true });
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult UpdateAssetCounts()
        {

            var startTicks = DateTime.Now.Ticks;
            this.S.SiteService.UpdateAllAssetCounts();
            var duration = TimeSpan.FromTicks(DateTime.Now.Ticks - startTicks).TotalSeconds.ToString();

            return Json(new { success = true, duration = duration }, JsonRequestBehavior.AllowGet);
        }
    }
}
