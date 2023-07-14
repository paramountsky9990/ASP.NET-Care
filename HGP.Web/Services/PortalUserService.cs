using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNet.Identity.MongoDB;
using AutoMapper;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Models;
using HGP.Web.Models.Account;
using HGP.Web.Models.Admin;
using HGP.Web.Models.Assets;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Newtonsoft.Json;
using System.Web.Configuration;

namespace HGP.Web.Services
{
    public class PortalUserServiceMappingProfile : Profile
    {
        public PortalUserServiceMappingProfile()
        {
            CreateMap<PortalUser, ProfileHomeModel>();
            CreateMap<PortalUser, EditContactInfoModel>();
            CreateMap<PortalUser, EditAddressModel>();
            CreateMap<PortalUser, EditPasswordModel>();
            CreateMap<PortalUser, EditManagerModel>();
        }
    }

    public class PortalUserService : UserManager<PortalUser>
    {
        public PortalUserService(IUserStore<PortalUser> store)
            : base(store)
        {
        }

        public async Task<IdentityResult> AddRecentlyViewedAsset(string userId, Asset asset)
        {
            var user = await FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                throw new InvalidOperationException("Invalid user Id");
            }

            // Only one of each asset at a time
            var anAsset = user.RecentlyViewed.FirstOrDefault(x => x.HitNumber == asset.HitNumber);
            if (anAsset != null)
                user.RecentlyViewed.Remove(anAsset);

            asset.Description = ""; // Do not save description into user document, too big
            user.RecentlyViewed.Insert(0, Mapper.Map<Asset, AdminAssetsHomeGridModel>(asset));

            return await UpdateAsync(user);           
        }

        public async Task<IdentityResult> AddRecentCategory(Site site, string userId, string categoryUri)
        {
            var user = await FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                throw new InvalidOperationException("Invalid user Id");
            }

            // Only one of each asset at a time
            var aCategory = user.RecentCategories.FirstOrDefault(x => String.Equals(x.UriString, categoryUri, StringComparison.CurrentCultureIgnoreCase));
            if (aCategory != null)
                user.RecentCategories.Remove(aCategory);

            var sourceCategory = site.Categories.FirstOrDefault(x => x.UriString.ToLower() == categoryUri.ToLower());
            if (sourceCategory != null)
            {
                var recent = new RecentCategory()
                {
                    Name = sourceCategory.Name,
                    UriString = sourceCategory.UriString,
                    Count = sourceCategory.Count,
                    PortalId = site.Id
                };
                user.RecentCategories.Insert(0, recent);                
            }


            return await UpdateAsync(user);
        }

        public async Task<IdentityResult> AddRecentSearch(Site site, string userId, string searchText)
        {
            var user = await FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                throw new InvalidOperationException("Invalid user Id");
            }

            // Only one of each search text at a time
            var aSearch = user.RecentSearches.FirstOrDefault(x => String.Equals(x, searchText, StringComparison.CurrentCultureIgnoreCase));
            if (aSearch != null)
                user.RecentSearches.Remove(aSearch);

            user.RecentSearches.Insert(0, searchText);

            return await UpdateAsync(user);
        }

        public AdminUsersHomeModel BuildAdminUsersHomeModel(string siteId)
        {
            var site = IoC.Container.GetInstance<ISiteService>().GetById(siteId);
            if (site == null)
                return null;

            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminUsersHomeModel>();

            var users = (from u in Users
                         where u.PortalId == siteId
                         orderby u.FirstName
                         select u);

            model.SiteId = siteId;
            model.PortalTag = site.SiteSettings.PortalTag;
            model.Users = users.ToModelList<PortalUserDto>();
            model.JsonData = JsonConvert.SerializeObject(model);
            model.CurrentDatabase = WebConfigurationManager.AppSettings["MongoDbName"];

            return model;
        }

        public AdminUsersPortalModel BuildAdminUsersPortalModel(string siteId)
        {
            var site = IoC.Container.GetInstance<ISiteService>().GetById(siteId);
            if (site == null)
                return null;

            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminUsersPortalModel>();

            var users = (from u in Users
                         where u.PortalId == siteId
                         orderby u.FirstName
                         select u);

            model.SiteId = siteId;
            model.PortalTag = site.SiteSettings.PortalTag;
            model.Users = users.ToModelList<PortalUserDto>();
            model.JsonData = JsonConvert.SerializeObject(model);

            return model;
        }

        public AdminUsersPortalModel BuildAdminUsersPortalModel(string siteId, int rows = 25, int page = 1)
        {
            var site = IoC.Container.GetInstance<ISiteService>().GetById(siteId);
            if (site == null)
                return null;

            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<AdminUsersPortalModel>();

            var users = (from u in Users
                         where u.PortalId == siteId
                         orderby u.FirstName
                         select u).Take(rows).Skip(page - 1);

            model.SiteId = siteId;
            model.PortalTag = site.SiteSettings.PortalTag;
            model.Users = users.ToModelList<PortalUserDto>();
            model.JsonData = JsonConvert.SerializeObject(model);

            return model;
        }

        public async Task<IdentityResult> AddUserToSite(string userId, Site site)
        {
            var user = await FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                throw new InvalidOperationException("Invalid user Id");
            }

            user.PortalId = site.Id;

            return await UpdateAsync(user);
        }

        public static PortalUserService Create(IdentityFactoryOptions<PortalUserService> options, IOwinContext context)
        {
            var manager = new PortalUserService(new UserStore<PortalUser>(context.Get<ApplicationIdentityContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<PortalUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };
            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 2,
                RequireNonLetterOrDigit = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
            };
            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;
            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug in here.
            manager.RegisterTwoFactorProvider("PhoneCode", new PhoneNumberTokenProvider<PortalUser>
            {
                MessageFormat = "Your security code is: {0}"
            });
            manager.RegisterTwoFactorProvider("EmailCode", new EmailTokenProvider<PortalUser>
            {
                Subject = "SecurityCode",
                BodyFormat = "Your security code is {0}"
            });
            //manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = new DataProtectorTokenProvider<PortalUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }

        /// <summary>
        /// Method to add user to multiple roles
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="roles">list of role names</param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddUserToRolesAsync(string userId, IList<string> roles)
        {
            var userRoleStore = (IUserRoleStore<PortalUser, string>)Store;

            var user = await FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                throw new InvalidOperationException("Invalid user Id");
            }

            var userRoles = await userRoleStore.GetRolesAsync(user).ConfigureAwait(false);
            // Add user to each role using UserRoleStore
            foreach (var role in roles.Where(role => !userRoles.Contains(role)))
            {
                await userRoleStore.AddToRoleAsync(user, role).ConfigureAwait(false);
            }

            // Call update once when all roles are added
            return await UpdateAsync(user).ConfigureAwait(false);
        }

        /// <summary>
        /// Remove user from multiple roles
        /// </summary>
        /// <param name="userId">user id</param>
        /// <param name="roles">list of role names</param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemoveUserFromRolesAsync(string userId, IList<string> roles)
        {
            var userRoleStore = (IUserRoleStore<PortalUser, string>)Store;

            var user = await FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                throw new InvalidOperationException("Invalid user Id");
            }

            var userRoles = await userRoleStore.GetRolesAsync(user).ConfigureAwait(false);
            // Remove user to each role using UserRoleStore
            foreach (var role in roles.Where(userRoles.Contains))
            {
                await userRoleStore.RemoveFromRoleAsync(user, role).ConfigureAwait(false);
            }

            // Call update once when all roles are removed
            return await UpdateAsync(user).ConfigureAwait(false);
        }

        internal async Task<IdentityResult> SetLastLogin(string userId, DateTime dateTime)
        {
            var user = await FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                throw new InvalidOperationException("Invalid user Id");
            }

            user.LastLogin = dateTime;

            return await UpdateAsync(user).ConfigureAwait(false);
        }

        internal void DeleteAllFromSite(string siteId)
        {
            var users = (from u in Users
                         where u.PortalId == siteId
                         select u);

            foreach (var user in users)
            {
                DeleteAsync(user);
            }
        }


        internal void DeleteFromSite(string siteId, string[] ids)
        {
            foreach (var id in ids)
            {
                var user = this.Users.FirstOrDefault(x => x.Id == id && x.PortalId == siteId);
                if (user != null)
                    this.DeleteAsync(user);
            }
        }

        internal List<string> GetIds(string siteId)
        {
            var users = (from u in Users
                         where u.PortalId == siteId 
                         select u.Id).ToList();

            return users;
        }

        internal async Task<ProfileHomeModel> BuildProfileHomeModel(ISite site, string userId)
        {
            var model = IoC.Container.GetInstance<ModelFactory>().GetModel<ProfileHomeModel>();

            var user = await FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                throw new InvalidOperationException("Invalid user Id");
            }

            Mapper.Map<PortalUser, ProfileHomeModel>(user, model);
            model.ContactInfoModel = Mapper.Map<PortalUser, EditContactInfoModel>(user);
            model.AddressModel = Mapper.Map<PortalUser, EditAddressModel>(user);
            model.PasswordModel = new EditPasswordModel(); // Don't copy in the existing password
            model.ManagerModel = Mapper.Map<PortalUser, EditManagerModel>(user);

            model.ContactInfoModel.UserId = userId;
            model.AddressModel.UserId = userId;
            model.PasswordModel.UserId = userId;
            model.ManagerModel.UserId = userId;

            return model;
        }
    }
}