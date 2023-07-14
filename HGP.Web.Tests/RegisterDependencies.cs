using System.Web;
using System.Web.Configuration;
using Amazon;
using Amazon.S3;
using AspNet.Identity.MongoDB;
using AutoMapper;
using HGP.Common.Database;
using HGP.Web.Controllers;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Services;
using Microsoft.AspNet.Identity;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.Diagnostics;
using StructureMap.Graph;
using StructureMap.Web;

namespace HGP.Web.Tests
{
    public class RegisterDependencies
    {
        public static void InitMongo()
        {
            //if (!BsonClassMap.IsClassMapRegistered(typeof(Currency)))
            //    BsonClassMap.RegisterClassMap<Currency>(cm => cm.MapField("value"));
        }

        public static void InitStructureMap()
        {
            var container = IoC.Initialize();
            IoC.Container = container;
            // Initialize IoC wrapper

            PerformRuntimeDepdendencyConfiguration(container);
            System.Diagnostics.Debug.WriteLine(IoC.Container.WhatDoIHave());
        }
       
        
        private static void PerformRuntimeDepdendencyConfiguration(IContainer container)
        {
            container.Configure(x => x.Scan(y =>
            {
                y.TheCallingAssembly();
                y.AssemblyContainingType<IBaseService>();
                y.AddAllTypesOf<BaseController>();
                y.WithDefaultConventions();
                y.LookForRegistries();
                y.AssemblyContainingType<IEmailService>();
            }));
          
        }

        public static void InitMapper()
        {
            Mapper.Initialize(cfg =>
                {
                    cfg.AddProfiles(typeof(HGP.Web.MvcApplication).Assembly);
                    cfg.IgnoreUnmapped();
                }
            );
        }
    }

    public class ApplicationRegistry : Registry
    {
        public ApplicationRegistry()
        {
            For<HttpContext>().HybridHttpOrThreadLocalScoped().Use(ctx => HttpContext.Current);
            For<IWorkContext>().HybridHttpOrThreadLocalScoped().Use<WorkContext>(); // todo: Verify correct scope objects are created
            For<IPortalServices>().HybridHttpOrThreadLocalScoped().Use<PortalServices>();
            For<IMongoRepository>().HybridHttpOrThreadLocalScoped().Use(new MongoRepository(WebConfigurationManager.AppSettings["MongoDbConnectionString"], WebConfigurationManager.AppSettings["MongoDbName"])); // todo: Why is this object registered as a Singleton?
            For(typeof(MongoCollection<>)).Use(new MongoCollectionInstanceFactory());
            For<IAmazonS3>().HybridHttpOrThreadLocalScoped().Use(new AmazonS3Client(RegionEndpoint.USWest1));
            For<IAwsService>().HybridHttpOrThreadLocalScoped().Use(x => new AwsService());
            For<IUserStore<PortalUser>>().Use<UserStore<PortalUser>>();
            //For<IAuthenticationManager>().Use(() => HttpContext.Current.GetOwinContext().Authentication);

        }
    }
}
