using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HGP.Web.Services
{
    //
    // Summary:
    //     Represents a status of Limilabs.Client.SMTP.ISendMessageResult.
    public enum SendMessageStatus
    {
        //
        // Summary:
        //     Success status: message was succesfully sent to all receipients.
        Success = 0,
        //
        // Summary:
        //     Partial success status: message was succesfully sent to some, but not to all
        //     receipients.
        PartialSucess = 1,
        //
        // Summary:
        //     Failure status: message sending failed.
        Failure = 2
    }

    public class EmailResult
    {
        public SendMessageStatus SendStatus { get; set; }
    }
}
