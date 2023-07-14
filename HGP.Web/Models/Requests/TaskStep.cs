using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Requests
{
    public class TaskStep
    {
        public string TaskType { get; set; }
        public string Action { get; set; }
        public string ActionResult { get; set; }
        public DateTime DatePerformed { get; set; }

    }
}