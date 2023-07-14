using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using HGP.Common;
using HGP.Web.Models;

namespace HGP.Web.Services
{

    public interface IActivityLogService
    {
        Task LogSearch(string portalTag, string userName, string searchTerm, int resultCount);
        Task LogCategoryBrowse(string portalTag, string userName, string category, int resultCount);
        Task LogLocationBrowse(string portalTag, string userName, string location, int resultCount);
        Task LogActivity(GlobalConstants.ActivityTypes activityType, Site site, PortalUser user, string data = "", string data2 = "");
        Task LogActivity(GlobalConstants.ActivityTypes activityType, Site site, string userName = "", string data = "", string data2 = "");
        Task LogActivity(GlobalConstants.ActivityTypes activityType, string portalTag = "", string userName = "", string data = "", string data2 = "");
    }

    public class ActivityLogService : BaseService<ActivityLog>, IActivityLogService
    {
        public ActivityLogService()
        {
        }

        public async Task LogSearch(string portalTag, string userName, string searchTerm, int resultCount)
        {
            await LogActivity(GlobalConstants.ActivityTypes.Search, portalTag, userName, searchTerm, resultCount.ToString());
        }

        public async Task LogCategoryBrowse(string portalTag, string userName, string category, int resultCount)
        {
            await LogActivity(GlobalConstants.ActivityTypes.Browse, portalTag, userName, category, resultCount.ToString());
        }

        public async Task LogLocationBrowse(string portalTag, string userName, string location, int resultCount)
        {
            await LogActivity(GlobalConstants.ActivityTypes.BrowseByLocation, portalTag, userName, location, resultCount.ToString());
        }

        public Task LogActivity(GlobalConstants.ActivityTypes activityType, Site site, PortalUser user, string data = "", string data2 = "")
        {
            return this.LogActivity(activityType, site.SiteSettings.PortalTag, user.FirstName + " " + user.LastName, data, data2);
        }

        public Task LogActivity(GlobalConstants.ActivityTypes activityType, Site site, string userName = "", string data = "", string data2 = "")
        {
            return this.LogActivity(activityType, site.SiteSettings.PortalTag, userName, data, data2);
        }


        public async Task LogActivity(GlobalConstants.ActivityTypes activityType, string portalTag = "", string userName = "", string data = "", string data2 = "")
        {
            var logEntry = new ActivityLog()
            {
                ActivityType = activityType.ToString(),
                PortalId = portalTag,
                UserId = userName,
                Data = data,
                Data2 = data2,
                CreatedBy = this.WorkContext.CurrentUser.Id
            };

            await Task.Factory.StartNew(() => this.Save(logEntry));
        }

    }
}