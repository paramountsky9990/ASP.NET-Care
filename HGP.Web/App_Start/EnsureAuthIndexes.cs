using System.Web.Configuration;
using AspNet.Identity.MongoDB;
using HGP.Common.Database;
using HGP.Web.Models;
using HGP.Web.Models.Report;
using MongoDB.Driver.Builders;

namespace HGP.Web
{
    public class EnsureIndexes
	{
		public static void Exist()
		{
			var context = ApplicationIdentityContext.Create();
            IndexChecks.EnsureUniqueIndexOnUserName(context.Users);
            IndexChecks.EnsureUniqueIndexOnEmail(context.Users);
			IndexChecks.EnsureUniqueIndexOnRoleName(context.Roles);

            var repo = new MongoRepository(WebConfigurationManager.AppSettings["MongoDbConnectionString"], WebConfigurationManager.AppSettings["MongoDbName"]);
		    var collection = repo.GetQuery<Asset>();

            var result = collection.CreateIndex(IndexKeys.Text("Title", "Manufacturer", "ModelNumber", "SerialNumber", "HitNumber", "ClientIdNumber"));
        }
	}
}