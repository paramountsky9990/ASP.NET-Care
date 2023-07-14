using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace HGP.Web.Models.Requests
{
    public class RequestIndexModel : PageModel
    {
        public Request Request { get; set; }

        [JsonIgnore]
        public string JsonData { get; set; }

        public RequestIndexModel()
        {
            this.SiteSettings = new SiteSettings();
        }
    }
}