using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Models.Admin;
using HGP.Web.Services;

namespace HGP.Web.Models
{
    public class ModelFactory
    {
        public class ModelFactoryMappingProfile : Profile
        {
            public ModelFactoryMappingProfile()
            {
                CreateMap<SiteSettings, SiteSettings>();
            }
        }

        public T GetModel<T>() where T : PageModel, new()
        {
            var model = new T();

            var portalServices = IoC.Container.GetInstance<IPortalServices>();
            if (portalServices.WorkContext.CurrentUser == null)
                if (portalServices.WorkContext.CurrentSite == null)
                    model.HeaderModel = portalServices.HeaderService.BuildHeaderModel();
                else
                {
                    model.HeaderModel =
                        portalServices.HeaderService.BuildHeaderModel(portalServices.WorkContext.CurrentSite.Id);
                    model.SiteSettings =
                        Mapper.Map<SiteSettings, SiteSettings>(portalServices.WorkContext.CurrentSite.SiteSettings);
                }
            else
            {
                model.HeaderModel = portalServices.HeaderService.BuildHeaderModel(portalServices.WorkContext.CurrentSite.Id, portalServices.WorkContext.CurrentUser.Id);
                model.SiteSettings =
                    Mapper.Map<SiteSettings, SiteSettings>(portalServices.WorkContext.CurrentSite.SiteSettings);
            }

            return model;
        }

        public void RebuildModel<T>(T model) where T : PageModel, new()
        {
            var portalServices = IoC.Container.GetInstance<IPortalServices>();
            if (portalServices.WorkContext.CurrentUser == null)
                model.HeaderModel = portalServices.HeaderService.BuildHeaderModel(portalServices.WorkContext.CurrentSite.Id);
            else
                model.HeaderModel = portalServices.HeaderService.BuildHeaderModel(portalServices.WorkContext.CurrentSite.Id, portalServices.WorkContext.CurrentUser.Id);
            model.SiteSettings = Mapper.Map<SiteSettings, SiteSettings>(portalServices.WorkContext.CurrentSite.SiteSettings);
        }
    }
}