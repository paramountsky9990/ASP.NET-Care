using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
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
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HGP.Web.Controllers
{
    public class AdminLocationsControllerMappingProfile : Profile
    {
        public AdminLocationsControllerMappingProfile()
        {
            CreateMap<LocationCreateModel, Location>();
            CreateMap<Location, LocationEditModel>();
            CreateMap<LocationEditModel, Location>();
        }
    }

    [Authorize(Roles = "SuperAdmin")]
    public class AdminLocationsController : BaseController
    {
        public static ILogger Logger { get; set; }

        public AdminLocationsController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("AdminLocationsController");
        }

        public ActionResult Index(string id)
        {
            var site = this.S.SiteService.GetByPortalTag(id);
            var model = this.S.SiteService.BuildAdminLocationsHomeModel(site.Id);
            return View(model);
        }

        [AcceptVerbs("GET")]
        public ActionResult Create(string id)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<LocationCreateModel>();
            var site = this.S.SiteService.GetByPortalTag(id);
            model.SiteId = site.Id;
            return View("~/views/adminlocations/create.cshtml", model);
        }

        // POST: AdminPortal/Create
        [AcceptVerbs("POST")]
        public ActionResult Create(LocationCreateModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var location = new Location();
                    Mapper.Map<LocationCreateModel, Location>(model, location);
                    var site = this.S.SiteService.GetById(model.SiteId);
                    site.Locations.Add(location);
                    this.S.SiteService.Save(site);

                    if ((List<AlertMessage>) TempData["messages"] != null)
                        ((List<AlertMessage>) TempData["messages"]).Add(new AlertMessage()
                        {
                            Severity = AlertSeverity.Success,
                            Message = "Location successfully created."
                        });

                    return RedirectToRoute("AdminPortalRoute",
                        new {controller = "AdminLocations", action = "Index", id = site.SiteSettings.PortalTag});
                }

                return View("~/views/adminlocations/create.cshtml", model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [AcceptVerbs("GET")]
        public ActionResult Edit(string id, string locationName)
        {
            var location = this.S.SiteService.GetLocationByName(id, locationName);
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<LocationEditModel>();
            Mapper.Map<Location, LocationEditModel>(location, model);
            model.OriginalName = model.Name;
            model.PortalTag = id;
            // The original name is used rto look up the location because we do not use location ids

            return View("~/views/adminlocations/edit.cshtml", model);
        }

        [AcceptVerbs("POST")]
        public ActionResult Edit(LocationEditModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var site = this.S.SiteService.GetByPortalTag(model.PortalTag);

                    var originalLocation = site.Locations.FirstOrDefault(x => x.Name == model.OriginalName);

                    Mapper.Map<LocationEditModel, Location>(model, originalLocation);

                    this.S.SiteService.Save((Site) site);
      
                    // Check for a location contact and make sure they have the "approver" role
                    if (!model.OwnerEmail.IsNullOrWhiteSpace())
                    {
                        var userManager = IoC.Container.GetInstance<PortalUserService>();
                        var user = userManager.FindByEmail(model.OwnerEmail);
                        if (user != null)
                        {
                            userManager.AddToRole(user.Id, "Approver");
                            if ((List<AlertMessage>)TempData["messages"] != null) ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage()
                            { Severity = AlertSeverity.Success, Message = "Location owner has been added to the Approver's list." });
                        }
                        else
                        {
                            if ((List<AlertMessage>)TempData["messages"] != null) ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage()
                            { Severity = AlertSeverity.Warning, Message = "Location owner was not found and cannot be added to the Approver's list." });
                        }
                    }

                    if ((List<AlertMessage>) TempData["messages"] != null) ((List<AlertMessage>) TempData["messages"]).Add(new AlertMessage()
                        { Severity = AlertSeverity.Success, Message = "Location successfully updated." });

                    return RedirectToRoute("AdminPortalRoute",
                        new {controller = "AdminLocations", action = "Index", id = site.SiteSettings.PortalTag});
                }

                return View("~/views/adminlocations/index.cshtml");
            }
            catch (Exception ex)
            {
                throw;

            }
        }

        [AcceptVerbs("POST")]
        public ActionResult Delete(string portalTag, string locationName)
        {
            var site = this.S.SiteService.GetByPortalTag(portalTag);
            var assetsDeleted = 0;

            var idsToDelete = this.S.AssetService.GetByLocation(site.Id, locationName).Select(x => x.HitNumber);
            if (idsToDelete.Any())
            {
                assetsDeleted = this.S.AssetService.DeleteAssets(site.Id, idsToDelete);
            }

            var originalLocation1 = site.Locations.FirstOrDefault(x => x.Name == locationName);
            var originalLocation = site.Locations.Remove(originalLocation1);

            this.S.SiteService.Save((Site) site);

            if ((List<AlertMessage>) TempData["messages"] != null) ((List<AlertMessage>) TempData["messages"]).Add(new AlertMessage(){ Severity = AlertSeverity.Success, Message = "Location removed. Assets removed: " + assetsDeleted.ToString() });

            return Json(new {success = true});
        }
    }
}