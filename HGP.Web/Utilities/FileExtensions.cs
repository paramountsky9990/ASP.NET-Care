using System.Web;

namespace HGP.Web.Utilities
{
    public static class FileExtensions
    {
        public static bool HasFile(this HttpPostedFileBase file)
        {
            return (file != null && file.ContentLength > 0) ? true : false;
        }
    }

}