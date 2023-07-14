using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace HGP.Web.Models.Requests
{
    public class RequestListModel : PageModel
    {
        public IList<Request> Requests { get; set; }

        [JsonIgnore]
        public string JsonData { get; set; }
        public RequestListModel()
        {
            this.Requests = new List<Request>();
        }
    }
}