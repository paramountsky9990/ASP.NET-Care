using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Common;
using HGP.Web.DependencyResolution;
using HGP.Web.Models;
using HGP.Web.Models.InBox;
using HGP.Common.Database;
using Microsoft.AspNet.Identity;

namespace HGP.Web.Services
{
    public interface IInBoxItemService : IBaseService
    {
        void Save(InboxItem entry);
        InboxItem GetById(string id);

        int GetInBoxCount(string siteId, string userId);
    }
    public class InBoxItemService : BaseService<InboxItem>, IInBoxItemService
    {
        public InBoxItemService(IRequestService requestService)
        {
            this.RequestService = requestService;
        }

        public int GetInBoxCount(string siteId, string userId)
        {
            var inBoxCount = (from i in Repository.All<InboxItem>()
                where i.PortalId == siteId && i.Status == GlobalConstants.InboxStatusTypes.Pending
                select i).Count();
            return inBoxCount;
        }

        public IRequestService RequestService { get; set; }
    }
}