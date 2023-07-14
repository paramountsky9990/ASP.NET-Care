using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.DynamicData;
using System.Web.Mvc;
using AutoMapper;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models.Account;
using HGP.Web.Services;
using log4net;
using log4net.Repository.Hierarchy;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using HGP.Web.Models;


//Change the loginUrl attribute to what you want it to be. Then, on the login action, select the proper view to return. 

//You'll need some type of identifier to distinguish clients (subdomain, cookie or something). Use that to select the proper view to return.


namespace HGP.Web.Controllers
{
    public class AccountControllerMappingProfile : Profile
    {
        public AccountControllerMappingProfile()
        {
            CreateMap<EditContactInfoModel, PortalUser>();
            CreateMap<EditAddressModel, PortalUser>();
            CreateMap<EditManagerModel, PortalUser>();
        }
    }

    [Authorize]
    public class AccountController : BaseController
    {
        private static ILogger Logger = Log4NetLogger.GetLogger();

        private PortalUserService userService;
        private HGP.Web.Services.IEmailService emailervice;

        public AccountController(IPortalServices portalServices)
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("AccountController");
        }

        public AccountController()
            : base(IoC.Container.GetInstance<IPortalServices>())
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("AccountController");
        }

        public AccountController(IPortalServices portalServices, PortalUserService userService, ApplicationSignInManager signInManager )
            : base(portalServices)
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("AccountController");
            UserService = userService;
            SignInManager = signInManager;
        }

        public PortalUserService UserService
        {
            get
            {
                return userService ?? HttpContext.GetOwinContext().GetUserManager<PortalUserService>();
            }
            private set
            {
                userService = value;
            }
        }

        public HGP.Web.Services.IEmailService EmailService
        {
            get
            {
                return emailervice ?? IoC.Container.GetInstance<IEmailService>();
            }
            private set
            {
                emailervice = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (this.S.WorkContext.CurrentSite.SiteSettings.IsAdminPortal)
            {
                var view = "~/Views/AdminAccount/Login.cshtml";
                var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminLoginViewModel>();
                return View(view, model);
            }
            else
            {
                var view = "~/Views/Account/Login.cshtml";
                var model = IoC.Container.GetInstance<ModelFactory>().GetModel<LoginViewModel>();
                model.ReturnUrl = returnUrl;
                return View(view, model);
            }
        }


        [AllowAnonymous]
        public ActionResult GuestLogin(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            var model = new GuestLoginViewModel();
            return View("~/Views/Account/GuestLogin.cshtml", model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GuestLogin(GuestLoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInHelper.PasswordSignIn(model.Email, model.Password, isPersistent: true, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    var user = await SignInHelper.UserService.FindByNameAsync(model.Email);
                    var site = this.S.SiteService.GetById(user.PortalId);
                    return Redirect(site.SiteSettings.PortalTag);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                //case SignInStatus.RequiresVerification:
                //    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt. Error 001");
                    return View(model);
            }
        }

        private SignInHelper _signInHelper;

        public SignInHelper SignInHelper
        {
            get
            {
                return _signInHelper ?? new SignInHelper(UserService, AuthenticationManager);
            }
            private set { _signInHelper = value; }
        }


        private ApplicationSignInManager _signInManager;

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set { _signInManager = value; }
        }


        public async Task<ActionResult> UserProfile()
        {
            var model = await this.UserService.BuildProfileHomeModel(this.S.WorkContext.CurrentSite, this.S.WorkContext.CurrentUser.Id);
            return View(model);
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditContactInfo(EditContactInfoModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserService.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
                }

                Mapper.Map<EditContactInfoModel, PortalUser>(model, user);
                user.EmailConfirmed = false;
                user.PhoneNumber = new string(model.PhoneNumber.Where(char.IsDigit).ToArray());

                await UserService.UpdateAsync(user);

                if ((List<AlertMessage>)TempData["messages"] != null)
                    ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Your contact info has been updated." });
                
                return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
            }

            return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAddress(EditAddressModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserService.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
                }

                Mapper.Map<EditAddressModel, PortalUser>(model, user);

                await UserService.UpdateAsync(user);

                if ((List<AlertMessage>)TempData["messages"] != null)
                    ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Your address info has been updated." });

                return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
            }

            return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditPassword(EditPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserService.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
                }

                var result = await UserService.ChangePasswordAsync(model.UserId, model.Password, model.NewPassword);
                if (result.Succeeded)
                {
                    await SignInHelper.SignInAsync(user, isPersistent: true, rememberBrowser: false);

                    if ((List<AlertMessage>)TempData["messages"] != null)
                        ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Your password has been updated." });

                    return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
                }

                AddErrors(result);
            }

            return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditManager(EditManagerModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserService.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
                }

                Mapper.Map<EditManagerModel, PortalUser>(model, user);

                await UserService.UpdateAsync(user);

                if ((List<AlertMessage>)TempData["messages"] != null)
                    ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Your manager has been updated." });

                return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
            }

            return RedirectToRoute("PortalRoute", new { controller = "Account", action = "UserProfile" });
        }
        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                IoC.Container.GetInstance<ModelFactory>().RebuildModel(model);
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInHelper.PasswordSignIn(model.Email, model.Password, isPersistent: true, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    if (string.IsNullOrEmpty(returnUrl))
                        returnUrl = "/" + this.S.WorkContext.CurrentSite.SiteSettings.PortalTag;
                    return Redirect(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                //case SignInStatus.RequiresVerification:
                //    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    IoC.Container.GetInstance<ModelFactory>().RebuildModel(model);
                    ModelState.AddModelError("", "Invalid login attempt.. Error 002");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            var user = await UserService.FindByIdAsync(await SignInManager.GetVerifiedUserIdAsync());
            if (user != null)
            {
                var code = await UserService.GenerateTwoFactorTokenAsync(user.Id, provider);
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case Microsoft.AspNet.Identity.Owin.SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case Microsoft.AspNet.Identity.Owin.SignInStatus.LockedOut:
                    return View("Lockout");
                case Microsoft.AspNet.Identity.Owin.SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<RegisterViewModel>();
            model.HeaderModel = this.S.HeaderService.BuildHeaderModel(this.S.WorkContext.CurrentSite.Id);
            model.RegistrationMessage = this.S.WorkContext.CurrentSite.SiteSettings.RegistrationMessage;
            return View(model);
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new PortalUser
                {
                    UserName = model.Email, 
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.Phone,
                    PortalId = this.S.WorkContext.CurrentSite.Id,
                    LastLogin = DateTime.UtcNow
                };
                var result = await UserService.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var site = this.S.SiteService.GetById(user.PortalId);
                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    string code = await userService.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = Url.RouteUrl("PortalRoute", new { controller = "Account", action = "ConfirmEmail", userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    await this.EmailService.SendWelcomeMessage(user, site, callbackUrl);

                    return RedirectToRoute("PortalRoute", new { controller = "Account", action = "ConfirmRegistration", userId = user.Id, code = code });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            return RedirectToRoute("PortalRoute", new {controller = "Account", action = "Login"});

            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<ConfirmEmailModel>();
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserService.ConfirmEmailAsync(userId, code);

            ActionResult view;
            if (result.Succeeded)
                view = RedirectToRoute("PortalRoute", new {controller = "Portal", action = "Index"});
            else
                view = View("Error", model);

            return view;
        }

        [AllowAnonymous]
        public ActionResult ConfirmRegistration()
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<ConfirmEmailModel>();

            return View(model);
        }


  
       
        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<ForgotPasswordViewModel>();

            return View(model);
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserService.FindByNameAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or account not found. Error x0001");
                    IoC.Container.GetInstance<ModelFactory>().RebuildModel(model);
                    return View("ForgotPassword", model);
                }

                var site = this.S.SiteService.GetById(user.PortalId);

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await userService.GeneratePasswordResetTokenAsync(user.Id);
                //string codeHtmlVersion = HttpUtility.UrlEncode(code);
                var callbackUrl = Url.RouteUrl("PortalRoute", new { controller = "Account", action = "ResetPassword", userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                await this.EmailService.SendResetPasswordNotification(user, site, callbackUrl);
                return RedirectToRoute("PortalRoute", new { controller = "Account", action = "ForgotPasswordConfirmation" });
            }

            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Invalid email or account not found. Error x0002");
            IoC.Container.GetInstance<ModelFactory>().RebuildModel(model);
            return View("ForgotPassword", model);
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult GuestForgotPassword()
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<GuestForgotPasswordViewModel>();

            return View(model);
        }
        
        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GuestForgotPassword(GuestForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserService.FindByNameAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                var site = this.S.SiteService.GetById(user.PortalId);

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await userService.GeneratePasswordResetTokenAsync(user.Id);
                //string codeHtmlVersion = HttpUtility.UrlEncode(code);
                var callbackUrl = Url.Action("GuestResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                await this.EmailService.SendResetPasswordNotification(user, site, callbackUrl);
                return RedirectToAction("GuestForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            IoC.Container.GetInstance<ModelFactory>().RebuildModel(model);
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<PasswordConfirmationModel>();
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult GuestForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code, string email)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<ResetPasswordViewModel>();
            model.Email = email;
            model.Code = code;
            return code == null ? View("Error") : View(model);
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult GuestResetPassword(string code, string email)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<GuestResetPasswordViewModel>();
            model.Email = email;
            model.Code = code;
            return code == null ? View("Error") : View(model);
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                IoC.Container.GetInstance<ModelFactory>().RebuildModel<ResetPasswordViewModel>(model);
                return View(model);
            }
            var user = await UserService.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToRoute("PortalRoute", new { controller = "Account", action = "Login" });
            }
            var result = await UserService.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                if ((List<AlertMessage>)TempData["messages"] != null)
                    ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Your password has been reset, please sign in using your new password." });
                return RedirectToRoute("PortalRoute", new { controller = "Account", action = "Login" });
            }
            AddErrors(result);
            IoC.Container.GetInstance<ModelFactory>().RebuildModel<ResetPasswordViewModel>(model);
            return View(model);
        }


        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> GuestResetPassword(GuestResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                IoC.Container.GetInstance<ModelFactory>().RebuildModel<GuestResetPasswordViewModel>(model);
                return View(model);
            }
            var user = await UserService.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToRoute("GuestRoute", new { controller = "Account", action = "GuestLogin" });
            }
            var result = await UserService.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                if ((List<AlertMessage>)TempData["messages"] != null)
                    ((List<AlertMessage>)TempData["messages"]).Add(new AlertMessage() { Severity = AlertSeverity.Success, Message = "Your password has been reset, please sign in using your new password." });
                return RedirectToRoute("GuestRoute", new { controller = "Account", action = "GuestLogin" });
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult GuestResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserService.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case Microsoft.AspNet.Identity.Owin.SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case Microsoft.AspNet.Identity.Owin.SignInStatus.LockedOut:
                    return View("Lockout");
                case Microsoft.AspNet.Identity.Owin.SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case Microsoft.AspNet.Identity.Owin.SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new PortalUser { UserName = model.Email, Email = model.Email };
                var result = await UserService.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserService.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: true, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();

            var route = RedirectToRoute("PortalRoute", new { controller = "Account", action = "Login", portalTag = this.S.WorkContext.CurrentSite.SiteSettings.PortalTag });
            if (this.S.WorkContext.CurrentSite != null)
                if (this.S.WorkContext.CurrentSite.SiteSettings.IsAdminPortal)
                    route = RedirectToRoute("PortalRoute", new { controller = "Account", action = "Login", portalTag = this.S.WorkContext.CurrentSite.SiteSettings.PortalTag }); 
            
            return route;
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}