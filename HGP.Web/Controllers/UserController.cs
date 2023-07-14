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
using HGP.Web.Models.User;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HGP.Web.Controllers
{
    public class UserControllerMappingProfile : Profile
    {
        public UserControllerMappingProfile()
        {
            CreateMap<AdminUsersPortalModel, PortalUser>();
            CreateMap<UserCreateModel, PortalUser>();
        }
    }

    [Authorize(Roles = "ClientAdmin, SuperAdmin")]
    public class UserController : BaseController
    {
        public static ILogger Logger { get; set; }
        public PortalUserService UserService { get; set; }
        public UserController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("UsersController");

            this.UserService = IoC.Container.GetInstance<PortalUserService>();
        }

        [OutputCache(CacheProfile = "ZeroCacheProfile")]
        public ActionResult Index(string id)
        {
            var model = this.UserService.BuildAdminUsersPortalModel(this.S.WorkContext.CurrentSite.Id);
            return View("~/views/user/index.cshtml", model);
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
                site = this.S.WorkContext.CurrentSite;
                model.SiteId = site.Id;
                return View("~/views/user/create.cshtml", model);
            }
            else
            {
                var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminUserCreateModel>();
                site = this.S.SiteService.GetByPortalTag(id);
                model.SiteId = site.Id;
                return View("~/views/user/create.cshtml", model);
            }



        }


        // POST: AdminPortal/Create
        [AcceptVerbs("POST")]
        public async Task<ActionResult> Create(AdminUsersPortalModel model)
        {
            var site = this.S.SiteService.GetById(model.SiteId);
            var userManager = IoC.Container.GetInstance<PortalUserService>();

            try
            {
                if (ModelState.IsValid)
                {
                    var user = new PortalUser();
                    Mapper.Map<AdminUsersPortalModel, PortalUser>(model, user);
                    Mapper.Map<UserCreateModel, PortalUser>(model.NewUserModel, user);
                    user.PortalId = model.SiteId;
                    user.UserName = model.NewUserModel.Email;
                    user.AddRole(model.NewUserModel.Role);
                    var result = await UserService.CreateAsync(user, "Welcome1!");

                    if (result.Succeeded)
                    {
                        UserService.AddToRole(user.Id, "Requestor");
                        await UserService.AddUserToSite(user.Id, site);

                        string callbackUrl;
                        if (model.NewUserModel.SendWelcomeMessage)
                        {
                            callbackUrl = Url.RouteUrl("PortalRoute", new { controller = "Account", action = "ConfirmEmail", userId = user.Id }, protocol: Request.Url.Scheme);
                            await this.S.EmailService.SendWelcomeMessage(user, site, callbackUrl);
                        }

                        if ((List<AlertMessage>)TempData["messages"] != null)
                            ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "User successfully created." });
                    }
                    else
                    {
                        if ((List<AlertMessage>)TempData["messages"] != null)
                            ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Error, Message = string.Format("Error occurred. {0}", result.Errors.First().ToString()) });
                    }

                    return RedirectToRoute("PortalRoute", new { controller = "User", action = "Index", id = site.SiteSettings.PortalTag });
                }

                return View("~/views/user/index.cshtml", model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        [HttpPost]
        public JsonResult DoesUserNameExist(AdminUsersPortalModel model)
        {
            var user = UserService.FindByEmail(model.NewUserModel.Email);

            return Json(user == null);
        }


        public ActionResult GridDeleteUsers(string idsToDelete)
        {
            var jsonIds = JsonConvert.DeserializeObject(idsToDelete);
            // you've got a list of strings so you can loop through them
            string[] ids = ((JArray)jsonIds)
                .Select(x => x.Value<string>())
                .ToArray();

            var userManager = IoC.Container.GetInstance<PortalUserService>();
            userManager.DeleteFromSite(this.S.WorkContext.CurrentSite.Id, ids);
            return Json(new { success = true });
        }
    }
}