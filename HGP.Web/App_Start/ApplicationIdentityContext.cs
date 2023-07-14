using System;
using System.Web.Configuration;
using AspNet.Identity.MongoDB;
using HGP.Web.Models;
using MongoDB.Driver;

namespace HGP.Web
{
    public class ApplicationIdentityContext : IdentityContext, IDisposable
    {
        public ApplicationIdentityContext(MongoCollection users, MongoCollection roles)
            : base(users, roles)
        {
        }

        public static ApplicationIdentityContext Create()
        {
            // todo add settings where appropriate to switch server & database in your own application
            var client = new MongoClient(WebConfigurationManager.AppSettings["MongoDbConnectionString"]); 
            var database = client.GetServer().GetDatabase(WebConfigurationManager.AppSettings["MongoDbName"]);
            var users = database.GetCollection<IdentityUser>("PortalUsers");
            var roles = database.GetCollection<IdentityRole>("Roles");
            return new ApplicationIdentityContext(users, roles);
        }

        public void Dispose()
        {
        }
    }
}
