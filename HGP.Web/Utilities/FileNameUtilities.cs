using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace HGP.Web.Utilities
{
    public static class FileNameUtilities
    {
        public static short ExtractSequenceNumber(string fileName)
        {
            short sequenceNumber = 1;
            var extension = Path.GetExtension(fileName);
            var flieNameNoExtension = Path.GetFileName(fileName).Replace(extension, "");

            var sepPosition = flieNameNoExtension.LastIndexOf("-", System.StringComparison.Ordinal);
            if (sepPosition == 0)
                sepPosition = flieNameNoExtension.LastIndexOf("_", System.StringComparison.Ordinal);
            if (sepPosition > 0)
            {
                var sequenceIsValid = false;
                var sequenceIdStr = flieNameNoExtension.Substring(sepPosition + 1, flieNameNoExtension.Length - sepPosition - 1);
                sequenceIsValid = short.TryParse(sequenceIdStr, out sequenceNumber);
                if (!sequenceIsValid)
                    sequenceNumber = 1;
            }

            return sequenceNumber;
        }

        public static string InsertSequenceNumber(string fileName, string sequence)
        {
            // Find the extension position
            var extension = Path.GetExtension(fileName);
            var flieNameNoExtension = Path.GetFileName(fileName)?.Replace(extension, "");
            var result = flieNameNoExtension + "-" + sequence + "." + extension;
            return result;
        }

        public static string ExtractHitNumber(string fileName)
        {
            var hitNum = "";
            var extension = Path.GetExtension(fileName);
            var flieNameNoExtension = Path.GetFileName(fileName).Replace(extension, "");

            var dashPosition = flieNameNoExtension.LastIndexOf("-", System.StringComparison.Ordinal);
            if (dashPosition > 0)
            {
                hitNum = flieNameNoExtension.Substring(0, dashPosition);
            }
            else
            {
                hitNum = flieNameNoExtension;
            }

            return hitNum;
        }


        public static string GetContentTypeFromExtension(string fileName)
        {
            var result = "";

            var extension = Path.GetExtension(fileName).Replace(".", "");
            switch (extension.ToLower())
            {
                case "jpeg":
                case "jpg":
                    result = "image/jpeg";
                    break;

                case "svg":
                    result = "image/svg+xml";
                    break;

                case "png":
                    result = "image/png";
                    break;

                case "gif":
                    result = "image/gif";
                    break;

                case "doc":
                case "docx":
                    result = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;

                case "xls":
                case "xlsx":
                    result = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;

                case "ppt":
                case "pptx":
                    result = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                    break;

                case "zip":
                    result = "application/zip";
                    break;

                case "pdf":
                    result = "application/pdf";
                    break;

                case "txt":
                    result = "text/plain";
                    break;

                case "csv":
                    result = "text/csv";
                    break;

                case "avi":
                    result = "video/x-msvideo";
                    break;

                case "mpg":
                case "mpeg":
                    result = "video/mpeg";
                    break;

                case "rm":
                    result = "application/vnd.rn-realmedia";
                    break;

                case "wmv":
                    result = "video/x-ms-wmv";
                    break;

                case "mov":
                    result = "video/quicktime";
                    break;

                default:
                    // Don't upload unknown file types - security risk
                    //okayToSave = false;
                    break;
            }

            return result;
        }

        public static bool IsImageFromExtension(string fileName)
        {
            var result = false;

            var extension = Path.GetExtension(fileName).Replace(".", "");
            switch (extension.ToLower())
            {
                case "jpeg":
                case "jpg":
                case "png":
                case "gif":
                case "svg":
                    result = true;
                    break;
            }

            return result;
        }


    }
}