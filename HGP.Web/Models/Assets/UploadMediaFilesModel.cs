using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Models.Assets
{
    public class UploadMediaFilesModel
    {
        public UploadMediaFilesModel()
        {
            this.MediaFiles = new List<MediaFile>();
            this.SortOrder = new List<string>();
            IsDirty = false;
        }
        public IList<MediaFile> MediaFiles { get; set; }

        public IList<string> SortOrder { get; set; }

        public bool IsDirty { get; set; }
    }
}