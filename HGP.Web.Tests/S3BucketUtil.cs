using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using HGP.Web.Services;

namespace HGP.Web.Tests
{
    internal class S3BucketUtil
    {
        public static void CreateRootBucket()
        {
            var service = new AwsService();
            try
            {
                var result = service.TryCreateRootBucket(WebConfigurationManager.AppSettings["AWSBucketName"]);
            }
            catch (Exception)
            {

                service.TryRemoveRootBucket(WebConfigurationManager.AppSettings["AWSBucketName"]);
                service.TryCreateRootBucket(WebConfigurationManager.AppSettings["AWSBucketName"]);
            }
        }


        public static void RemoveRootBucket()
        {
            var service = new AwsService();
            service.TryRemoveRootBucket(WebConfigurationManager.AppSettings["AWSBucketName"]);
        }
    }
}
