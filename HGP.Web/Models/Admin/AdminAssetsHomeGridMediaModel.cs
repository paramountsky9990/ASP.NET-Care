using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Admin
{
    public class AdminAssetsHomeGridMediaModel
    {
        public string FileName { get; set; }
        public bool IsImage { get; set; }
        public string ContentType { get; set; }
        public short SortOrder { get; set; }
        public string Url { get; set; }
    }
}