using HGP.Common;
using HGP.Web.DependencyResolution;
using HGP.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static HGP.Common.GlobalConstants;

namespace HGP.Web.Services
{
    public interface IUnsubscribeService : IBaseService
    {
        void Save(Unsubscribe entry);
        Unsubscribe GetById(string id);

        bool Add(Unsubscribe unsubMail);
        Unsubscribe GetByPortalIdUserId(string portalId, string portalUserId);
        Unsubscribe GetByPortalIdUserIdUserEmail(string portalId, string portalUserId, string userEmail);

        UnsubscribeHomeModel BuildUnsubscribeHomeModel();
    }

    public class UnsubscribeService : BaseService<Unsubscribe>, IUnsubscribeService
    {
        public bool Add(Unsubscribe unsubMail)
        {
            bool res = false;
            try
            {
                Unsubscribe cUnsubMail = this.GetByPortalIdUserId(unsubMail.PortalId, unsubMail.PortalUserId);
                if (cUnsubMail == null)
                {
                    this.Save(unsubMail);
                }
                else
                {
                    if (unsubMail.MailType != cUnsubMail.MailType)
                    {
                        cUnsubMail.MailType = unsubMail.MailType;
                        this.Save(cUnsubMail);
                    }
                }
                res = true;
            }
            catch (Exception ex) { throw; }
            return res;
        }

        public UnsubscribeHomeModel BuildUnsubscribeHomeModel()
        {
            UnsubscribeHomeModel model = new UnsubscribeHomeModel();
            try
            {     
                model = IoC.Container.GetInstance<ModelFactory>().GetModel<UnsubscribeHomeModel>();

                Unsubscribe unsubscModel = this.GetByPortalIdUserId(this.WorkContext.CurrentSite.Id, this.WorkContext.CurrentUser.Id);
                if (unsubscModel == null)
                {
                     unsubscModel = new Unsubscribe()
                    {
                        MailType = GlobalConstants.UnsubscribeTypes.ReceiveAll,
                        PortalId = this.WorkContext.CurrentSite.Id,
                        PortalUserId = this.WorkContext.CurrentUser.Id,
                        PortalUserEmail = this.WorkContext.CurrentUser.Email
                    };
                }

                model.Unsubscribe = unsubscModel;

            }
            catch (Exception ex) { }
            return model;
        }

        public Unsubscribe GetByPortalIdUserId(string portalId, string portalUserId)
        {
            Unsubscribe unsubMail = null;
            try
            {
                unsubMail = this.Repository.All<Unsubscribe>()
                    .FirstOrDefault(u => u.PortalId == portalId
                     && u.PortalUserId == portalUserId);
            }
            catch (Exception ex) { throw; }
            return unsubMail;
        }

        public Unsubscribe GetByPortalIdUserIdUserEmail(string portalId, string portalUserId, string userEmail)
        {
            Unsubscribe unsubMail = null;
            try
            {
                unsubMail = this.Repository.All<Unsubscribe>()
                    .FirstOrDefault(u => u.PortalId == portalId
                     && u.PortalUserId == portalUserId
                     && u.PortalUserEmail == userEmail);

            }
            catch (Exception ex) { throw; }
            return unsubMail;
        }
    }
}