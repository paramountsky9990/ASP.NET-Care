using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Services;

namespace HGP.Web.Security
{
    public class FilterDomainsAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            bool result = false;

            if (value == null)
            {
                return true;
            }

            var context = IoC.Container.GetInstance<WorkContext>();
            var route = context.HttpContext.Request.RequestContext.RouteData.Values.FirstOrDefault(x => x.Key == "portaltag");
            if (route.Value == null)
                return false;
            var site = IoC.Container.GetInstance<SiteService>().GetByPortalTag((string)route.Value);
            if (site == null)
                return false;
            var acceptedDomains = site.SiteSettings.EmailFilter;
            acceptedDomains += ",@hgpauction.com,@matrix6.com,@hginc.com"; // Make sure we can register with our own accounts
            if (string.IsNullOrEmpty(acceptedDomains))
            {
                result = true;
            }
            else
            {
                char[] delimiters = new char[2] { ';', ',' };
                var domainList = acceptedDomains.Split(delimiters);
                
                result = domainList.Any(x => value.ToString().ToLower().Contains(x));               
            }

            return result;
        }
    }
}