using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.ScheduledJob
{
    public sealed class PendingRequestReminderData
    {
        public List<SitePendingRequests> SitePendingRequests { get; set; }
    }

    public sealed class SitePendingRequests
    {
        public string site { get; set; }
        public List<string> requestIDs { get; set; }
        public DateTime reminderDate { get; set; }
    }
}