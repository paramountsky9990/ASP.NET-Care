using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using Glimpse.Core.Extensions;
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
    public class AdminUsersControllerMappingProfile : Profile
    {
        public AdminUsersControllerMappingProfile()
        {
            CreateMap<AdminUserCreateModel, PortalUser>();
        }
    }

    [Authorize(Roles = "SuperAdmin")]
    public class AdminUsersController : BaseController
    {
        public static ILogger Logger { get; set; }
        public PortalUserService UserService { get; set; }
        public AdminUsersController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("AdminUsersController");

            this.UserService = IoC.Container.GetInstance<PortalUserService>();
        }

        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index(string id)
        {
            ISite site = null;
            if (string.IsNullOrEmpty(id))
            {
                site = this.S.SiteService.GetByPortalTag("admin");
                var model = this.UserService.BuildAdminUsersHomeModel(site.Id);
                return View("~/views/adminusers/adminindex.cshtml", model);
            }
            else
            {
                site = this.S.SiteService.GetByPortalTag(id);
                var model = this.UserService.BuildAdminUsersHomeModel(site.Id);
                return View("~/views/adminusers/index.cshtml", model);
            }
        }

        [Authorize(Roles = "ClientAdmin, SuperAdmin")]
        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult PortalIndex(string id)
        {
            var model = this.UserService.BuildAdminUsersPortalModel(this.S.WorkContext.CurrentSite.Id);
            return View("~/views/adminusers/portalindex.cshtml", model);
        }

        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        [Authorize(Roles = "ClientAdmin, SuperAdmin")]
        public ActionResult GetUsersData(int rows, int page)
        {
            rows = 2000; // Asset grid preloads up to 2000 rows
            var model = this.UserService.BuildAdminUsersPortalModel(this.S.WorkContext.CurrentSite.Id, rows, page);
            return Json(model, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs("GET")]
        public ActionResult Create(string id)
        {
            ISite site = null;
            if (string.IsNullOrEmpty(id))
            {
                var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminUserCreateModel>();
                site = this.S.SiteService.GetByPortalTag("admin");
                model.SiteId = site.Id;
                return View("~/views/adminusers/createadmin.cshtml", model);
            }
            else
            {
                var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminUserCreateModel>();
                site = this.S.SiteService.GetByPortalTag(id);
                model.SiteId = site.Id;
                return View("~/views/adminusers/create.cshtml", model);
            }
        }

        [AcceptVerbs("GET")]
        public ActionResult CreateAdmin()
        {
            ISite site = null;

            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminUserCreateModel>();
            site = this.S.SiteService.GetByPortalTag("admin");
            model.SiteId = site.Id;
            return View("~/views/adminusers/createadmin.cshtml", model);

        }

        // POST: AdminPortal/Create
        [AcceptVerbs("POST")]
        public async Task<ActionResult> Create(AdminUserCreateModel model)
        {
            var site = this.S.SiteService.GetById(model.SiteId);

            try
            {
                if (ModelState.IsValid)
                {
                    var user = new PortalUser();
                    Mapper.Map<AdminUserCreateModel, PortalUser>(model, user);
                    user.PortalId = model.SiteId;
                    user.UserName = model.Email;
                    user.AddRole(model.Role);
                    var result = await UserService.CreateAsync(user, "Welcome1!");

                    if (result.Succeeded)
                    {
                        UserService.AddToRole(user.Id, "Requestor");
                        await UserService.AddUserToSite(user.Id, site);

                        if (model.SendWelcomeMessage)
                            await this.S.EmailService.SendWelcomeMessage4AdminUser(user, site);

                        if ((List<AlertMessage>)TempData["messages"] != null)
                            ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "User successfully created." });
                    }
                    else
                    {
                        if ((List<AlertMessage>)TempData["messages"] != null)
                            ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Error, Message = string.Format("Error occurred. {0}", result.Errors.First().ToString()) });
                    }

                    return RedirectToRoute("AdminPortalRoute", new { controller = "AdminUsers", action = "Index", id = site.SiteSettings.PortalTag });
                }

                return View("~/views/adminusers/create.cshtml", model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // POST: AdminPortal/Create
        [AcceptVerbs("POST")]
        public async Task<ActionResult> CreateAdmin(AdminUserCreateModel model)
        {
            var site = this.S.SiteService.GetByPortalTag("admin");

            try
            {
                if (ModelState.IsValid)
                {
                    var user = new PortalUser();
                    Mapper.Map<AdminUserCreateModel, PortalUser>(model, user);
                    user.PortalId = site.Id;
                    user.UserName = model.Email;
                    user.AddRole("SuperAdmin");
                    var result = await UserService.CreateAsync(user, "Welcome1!");

                    if (result.Succeeded)
                    {
                        await UserService.AddUserToSite(user.Id, (Site)site);

                        if (model.SendWelcomeMessage)
                            await this.S.EmailService.SendWelcomeMessage4AdminUser(user, (Site)site);

                        if ((List<AlertMessage>)TempData["messages"] != null)
                            ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "User successfully created." });
                    }
                    else
                    {
                        if ((List<AlertMessage>)TempData["messages"] != null)
                            ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Error, Message = string.Format("Error occurred. {0}", result.Errors.First().ToString()) });
                    }

                    return RedirectToRoute("AdminPortalRoute", new { controller = "AdminUsers", action = "Index" });
                }

                return View("~/views/adminusers/createadmin.cshtml", model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost]
        public JsonResult DoesUserNameExist(string email)
        {
            var user = UserService.FindByEmail(email);

            return Json(user == null);
        }


    }
}