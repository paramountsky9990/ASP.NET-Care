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
using System.Web.Configuration;
using MongoDB.Driver;
using AspNet.Identity.MongoDB;
using HGP.Web.Models;
using HGP.Web.Services;
using StructureMap;
using StructureMap.Web;

namespace HGP.Web.Tests.DependencyResolution {
    using StructureMap;
	
    public static class IoC {
        public static IContainer Container { get; set; }

        public static IContainer Initialize() {
            Container = new Container(c => c.AddRegistry<DefaultRegistry>());

            var client = new MongoClient(WebConfigurationManager.AppSettings["MongoDbConnectionString"]);
            var database = client.GetServer().GetDatabase(WebConfigurationManager.AppSettings["MongoDbName"]);
            var users = database.GetCollection<IdentityUser>("PortalUsers");
            var roles = database.GetCollection<IdentityRole>("Roles");
            var userStore = new UserStore<PortalUser>(new ApplicationIdentityContext(users, roles));
            var userManager = new PortalUserService(userStore);

            Container.Configure(x => x.For<PortalUserService>().HybridHttpOrThreadLocalScoped().Use(() => userManager));

            return Container;
        }
    }
}