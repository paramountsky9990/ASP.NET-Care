using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using HGP.Web.Extensions;

namespace HGP.Web
{
    public class MapperConfig
    {
        public static void Configure()
        {
            Mapper.Initialize(cfg =>
                {
                    cfg.AddProfiles(typeof(HGP.Web.MvcApplication).Assembly);
                    cfg.IgnoreUnmapped();
                }
            );
        }
    }
}