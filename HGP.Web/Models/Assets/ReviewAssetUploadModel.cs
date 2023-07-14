using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using HGP.Web.Controllers;
using Newtonsoft.Json;

namespace HGP.Web.Models.Assets
{
    public enum FileParseErrorType
    {
        General = 1,
        MissingLocation,
        OwnerNotFound,
        BadValue
    }

    public class FileParseError
    {
        [DisplayName("Line")]
        public int? LineNumber { get; set; }
        public FileParseErrorSeverity Severity { get; set; }
        public FileParseErrorType ErrorType { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }

        public FileParseError()
        {
            this.ErrorType = FileParseErrorType.General;
        }
    }

    public enum FileParseErrorSeverity
    {
        Warning = 1,
        Error,
        Fatal
    }

    public class ReviewAssetUploadModel : PageModel
    {
        public ReviewAssetUploadModel()
        {
            this.Errors = new List<FileParseError>();
        }

        [DisplayName("File")]
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string FullFileName { get; set; }
        public string ErrorFullFileName { get; set; }
        [DisplayName("Assets")]
        public int Rows { get; set; }
        public int Columns { get; set; }
        public ImportDatasetResult ImportResult { get; set; }
        public string SiteId { get; set; }

        public IList<FileParseError> Errors { get; set; }
        [JsonIgnore]
        public string JsonData { get; set; }
    }
}