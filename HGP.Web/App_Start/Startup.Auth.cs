using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web;
using Glimpse.Core.Extensions;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using HGP.Web.Models;

namespace HGP.Web
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Configure the db context, user manager and signin manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationIdentityContext.Create);
            app.CreatePerOwinContext<ApplicationRoleManager>(ApplicationRoleManager.Create);
            app.CreatePerOwinContext<PortalUserService>(PortalUserService.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/{0}/Account/Login"),
                ExpireTimeSpan = TimeSpan.FromDays(90),
                SlidingExpiration = true,
                Provider = new CookieAuthenticationProvider
                {
                    OnApplyRedirect = ApplyRedirect,
                    // Enables the application to validate the security stamp when the user logs in.
                    // This is a security feature which is used when you change a password or add an external login to your account.  
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<PortalUserService, PortalUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });            
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enables the application to temporarily store user information when they are verifying the second factor in the two-factor authentication process.
            //app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Enables the application to remember the second login verification factor such as phone or email.
            // Once you check this option, your second step of verification during the login process will be remembered on the device where you logged in from.
            // This is similar to the RememberMe option when you log in.
            //app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            //app.UseFacebookAuthentication(
            //   appId: "",
            //   appSecret: "");

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = "",
            //    ClientSecret = ""
            //});
        }

        /// <summary>
        /// Calulate a redirect url that contains the portal tag
        /// Requires that context.Options.LoginPath be in this format: /{0}/.....
        /// Where {0} equals the portal tag
        /// </summary>
        /// <param name="context"></param>
        private static void ApplyRedirect(CookieApplyRedirectContext context)
        {
            Contract.Requires(context.Options.LoginPath.ToString().Contains("{0}"));

            var loginUrl = HttpUtility.UrlDecode(context.Options.LoginPath.ToString());
            
            Uri absoluteUri;
            if (Uri.TryCreate(context.RedirectUri, UriKind.Absolute, out absoluteUri))
            {
                var queryStr = QueryString.FromUriComponent(absoluteUri).Value;
                queryStr = System.Uri.UnescapeDataString(queryStr);
                var portalTagsCollection = HttpUtility.ParseQueryString(queryStr);
                var portalTag = "";
                if (portalTagsCollection.HasKeys())
                {
                    var tagValues = portalTagsCollection.GetValues(0);
                    var charSeparators = new char[] {'/'};
                    var tags = tagValues.First().Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
                    portalTag = tags.FirstOrDefault(); // todo: handle null error
                }

                var path = PathString.FromUriComponent(absoluteUri);
                if (path == context.OwinContext.Request.PathBase + context.Options.LoginPath)
                    context.RedirectUri = string.Format(loginUrl, portalTag) +
                        new QueryString(
                            context.Options.ReturnUrlParameter,
                            context.Request.Uri.AbsoluteUri);
            }

            context.Response.Redirect(context.RedirectUri);
        }
    }
}