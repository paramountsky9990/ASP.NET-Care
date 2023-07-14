// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IoC.cs" company="Web Advanced">
// Copyright 2012 Web Advanced (www.webadvanced.com)
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


using System;
using System.Web;
using System.Web.Configuration;
using Amazon;
using Amazon.S3;
using AspNet.Identity.MongoDB;
using HGP.Common.Database;
using HGP.Web.Database;
using HGP.Web.Infrastructure;
using HGP.Web.Models;
using HGP.Web.Services;
using Microsoft.Ajax.Utilities;

// From https://github.com/webadvanced/Structuremap.MVC5
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using MongoDB.Driver;
using StructureMap.Building;
using StructureMap.Configuration.DSL;
using StructureMap.Graph;
using StructureMap.Pipeline;
using StructureMap.Web;
using Microsoft.Owin.Security;
using StructureMap.Web.Pipeline;

namespace HGP.Web.DependencyResolution {
    using StructureMap;
	
    public static class IoC {
        public static IContainer Container { get; set; }
        public static IContainer Initialize()
        {
            Container = new Container(c => c.Scan(scan =>
            {
                scan.TheCallingAssembly();
                scan.WithDefaultConventions();
                scan.LookForRegistries();
                scan.With(new ControllerConvention());
                scan.AddAllTypesOf<IBaseService>();
                scan.AssemblyContainingType<IEmailService>();
            }));

            var client = new MongoClient(WebConfigurationManager.AppSettings["MongoDbConnectionString"]);
            var database = client.GetServer().GetDatabase(WebConfigurationManager.AppSettings["MongoDbName"]); 
            var users = database.GetCollection<IdentityUser>("PortalUsers");
            var roles = database.GetCollection<IdentityRole>("Roles");
            var userStore = new UserStore<PortalUser>(new ApplicationIdentityContext(users, roles));
            var userManager = new PortalUserService(userStore);

            Container.Configure(x => x.For<PortalUserService>().HybridHttpOrThreadLocalScoped().Use(() => HttpContext.Current.GetOwinContext().GetUserManager<PortalUserService>()));

            //Container.Configure(x => x.For<PortalUserService>().HybridHttpOrThreadLocalScoped().Use(userManager));

            return Container;
        }
    }

    public class MyDefaultRegistry : Registry
    {
        public MyDefaultRegistry()
        {
            For<HttpContext>().HybridHttpOrThreadLocalScoped().Use(ctx => HttpContext.Current);
            For<IWorkContext>().HybridHttpOrThreadLocalScoped().Use<WorkContext>(); // todo: Verify correct scope objects are created
            For<IPortalServices>().HybridHttpOrThreadLocalScoped().Use<PortalServices>();
            For<IMongoRepository>().HybridHttpOrThreadLocalScoped().Use(new MongoRepository(WebConfigurationManager.AppSettings["MongoDbConnectionString"], WebConfigurationManager.AppSettings["MongoDbName"])); // todo: Why is this object registered as a Singleton?
            For(typeof(MongoCollection<>)).Use(new MongoCollectionInstanceFactory());
            For<IAmazonS3>().HybridHttpOrThreadLocalScoped().Use(new AmazonS3Client(RegionEndpoint.USWest1));
            For<IAwsService>().HybridHttpOrThreadLocalScoped().Use(x => new AwsService());
            For<IUserStore<PortalUser>>().Use<UserStore<PortalUser>>();
            For<IAuthenticationManager>().Use(() => HttpContext.Current.GetOwinContext().Authentication);
        }
    }

    public class MongoCollectionInstanceFactory : Instance
    {
        public override IDependencySource ToDependencySource(Type pluginType)
        {
            throw new NotImplementedException();
        }

        public override Instance CloseType(Type[] types)
        {
            var instanceType = typeof(MongoCollectionInstance<>).MakeGenericType(types);
            return new ObjectInstance(instanceType);
        }

        public override string Description
        {
            get { return "Build MongoCollection<T>() with MongoCollectionBuilder"; }
        }

        public override Type ReturnedType
        {
            get { return typeof(MongoCollection<>); }
        }
    }

    public class MongoCollectionInstance<T> : LambdaInstance<MongoCollection<T>> where T : class, new()
    {
        public MongoCollectionInstance()
            : base(c => c.GetInstance<IMongoRepository>().GetQuery<T>())
        {
        }
    }
}