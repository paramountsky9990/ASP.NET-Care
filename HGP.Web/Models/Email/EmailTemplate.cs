using HGP.Common.Database;

namespace HGP.Web.Models.Email
{
    public class EmailTemplate : MongoObjectBase
    {
        public string PortalId { get; set; }
        public string TemplateType { get; set; }
        public string Data { get; set; }
    }
}
