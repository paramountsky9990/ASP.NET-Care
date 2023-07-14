using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HGP.Web.Startup))]
namespace HGP.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
