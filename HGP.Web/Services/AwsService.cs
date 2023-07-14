using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;
using HGP.Common.Logging;
using HGP.Web.DependencyResolution;
using HGP.Web.Extensions;
using HGP.Web.Infrastructure;
using log4net.Repository.Hierarchy;
using Microsoft.Owin.Security.Provider;

namespace HGP.Web.Services
{
    public interface IAwsService
    {
        IAmazonS3 S3Client { get; }
        int TryCreateBucket(string bucketName);
        int TryRemoveBucket(string bucketName);

        void PutFile(string portalTag, string folder, string fileName, string contentType, MemoryStream fileData);
        Stream GetFile(string portalTag, string folder, string fileName);
        void PutRootFile(string portalTag, string fileName, string contentType, MemoryStream fileData);

        void PutUserRootFile(string portalTag, string userId, string fileName, string contentType, MemoryStream fileData);
        void PutUserFile(string portalTag, string userId, string folder, string fileName, string contentType, MemoryStream fileData);
        void CopyUserFile(string portalTag, string userId, string fileName, string destFileName);
    }

    public class AwsService : IAwsService
    {
        public static ILogger Logger { get; set; }
        public IAmazonS3 S3Client { get; private set; } 
        public string RootBucket { get; private set; }
        public AwsService()
        {
            Logger = Log4NetLogger.GetLogger();
            Logger.Information("UploadController");

            this.RootBucket = WebConfigurationManager.AppSettings["AWSBucketName"];
            Contract.Assert(!string.IsNullOrEmpty(RootBucket));
            this.S3Client = IoC.Container.GetInstance<IAmazonS3>();
        }

        /// <summary>
        /// Creates an S3 root level bucket for media if it doesn't exist
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns>1 = Success, 0 = failure</returns>
        public int TryCreateRootBucket(string bucketName)
        {
            try
            {
                var response = S3Client.PutBucket(new PutBucketRequest() { BucketName = this.RootBucket, BucketRegion = S3Region.USW1, CannedACL = S3CannedACL.PublicRead });
                // creating bucket
            }
            catch (Exception ex)
            {
                throw;
            }
            return 1;
        }

        public int TryRemoveRootBucket(string bucketName)
        {
            try
            {
                var request = new ListObjectsRequest
                {
                    BucketName = bucketName,
                    MaxKeys = int.MaxValue
                };

                var keyList = new List<KeyVersion>();

                do
                {
                    ListObjectsResponse response = S3Client.ListObjects(request);

                    // Process response.
                    foreach (var entry in response.S3Objects)
                    {
                        var key = new KeyVersion() { Key = entry.Key, VersionId = null };
                        keyList.Add(key);
                    }

                    // If response is truncated, set the marker to get the next 
                    // set of keys.
                    if (response.IsTruncated)
                    {
                        request.Marker = response.NextMarker;
                    }
                    else
                    {
                        request = null;
                    }
                } while (request != null);

                if (keyList.Any())
                {
                    foreach (var keys in keyList.Partition(900))
                    {
                        S3Client.DeleteObjects(new DeleteObjectsRequest() { BucketName = this.RootBucket, Objects = keys.ToList() });
                    }
                }
                var deleteResponse2 = S3Client.DeleteBucket(new DeleteBucketRequest() { BucketName = this.RootBucket });
            }
            catch (Exception ex)
            {
                throw;
            }
            return 1;
        }
        
        /// <summary>
        /// Creates an S3 bucket for media if it doesn't exist
        /// </summary>
        /// <param name="bucketName"></param>
        /// <returns>1 = Success, 0 = failure</returns>
        public int TryCreateBucket(string bucketName)
        {
            try
            {
                var response = S3Client.PutBucket(new PutBucketRequest() { BucketName = this.RootBucket + @"/" + bucketName + @"/", BucketRegion = S3Region.USW1, CannedACL = S3CannedACL.PublicRead });
                // creating bucket
            }
            catch (Exception ex)
            {
                throw;

            }
            return 1;
        }

        public void PutFile(string portalTag, string folder, string fileName, string contentType, MemoryStream fileData)
        {
            try
            {
                var request = new PutObjectRequest()
                {
                    BucketName = this.RootBucket + "/" + portalTag,
                    Key = folder + @"/" + fileName,
                    ContentType = contentType,
                    InputStream = fileData,
                    CannedACL = S3CannedACL.PublicRead
                };
                var response = S3Client.PutObject(request);
            }
            catch (AmazonS3Exception ex)
            {
                Logger.Debug(ex, "Error in PutFile");
                throw;
            }

        }

        public Stream GetFile(string portalTag, string folder, string fileName)
        {
            GetObjectResponse response;

            try
            {
                var request = new GetObjectRequest()
                {
                    BucketName = this.RootBucket + "/" + portalTag + folder,
                    Key =  fileName
                };
                response = S3Client.GetObject(request);
            }
            catch (AmazonS3Exception ex)
            {
                Logger.Debug(ex, "Error in GetFile");
                throw;
            }

            return response.ResponseStream;

        }

        public void PutUserFile(string portalTag, string userId, string folder, string fileName, string contentType, MemoryStream fileData)
        {
            try
            {
                var request = new PutObjectRequest()
                {
                    BucketName = this.RootBucket + "/" + portalTag + "/drafts/" + userId,
                    Key = folder + @"/" + fileName,
                    ContentType = contentType,
                    InputStream = fileData,
                    CannedACL = S3CannedACL.PublicRead
                };
                var response = S3Client.PutObject(request);
            }
            catch (AmazonS3Exception ex)
            {
                Logger.Debug(ex, "Error in PutFile");
                throw;
            }

        }

        public void CopyUserFile(string portalTag, string userId, string srcFileName, string destFileName)
        { 
            try
            {
                CopyObjectRequest request = new CopyObjectRequest
                {
                    SourceBucket = this.RootBucket + "/" + portalTag + "/drafts/" + userId,
                    SourceKey = @"/" + srcFileName,
                    DestinationBucket = this.RootBucket + "/" + portalTag,
                    DestinationKey = @"/" + destFileName,
                };
                CopyObjectResponse response = S3Client.CopyObject(request);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }

        public int DeleteFiles(string portalTag, List<string> fileNames)
        {
            var result = 0;
            var keyList = new List<KeyVersion>();

            if (fileNames.Any())
            {
                foreach (var file in fileNames)
                {
                    var key = new KeyVersion() { Key = "" + portalTag + "/i/" + file, VersionId = null };
                    keyList.Add(key);
                    key = new KeyVersion() { Key = "" + portalTag + "/t/" + file, VersionId = null };
                    keyList.Add(key);
                    key = new KeyVersion() { Key = "" + portalTag + "/l/" + file, VersionId = null };
                    keyList.Add(key);
                }

                foreach (var keys in keyList.Partition(900))
                {
                    try
                    {
                        var response = S3Client.DeleteObjects(new DeleteObjectsRequest() { BucketName = this.RootBucket, Objects = keys.ToList() });
                        result += response.DeletedObjects.Count;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }

            return result;
        }

        public void PutRootFile(string portalTag, string fileName, string contentType, MemoryStream fileData)
        {
            var request = new PutObjectRequest()
            {
                BucketName = this.RootBucket + "/" + portalTag,
                Key = fileName,
                ContentType = contentType,
                InputStream = fileData,
                CannedACL = S3CannedACL.PublicRead
            };
            var response = S3Client.PutObject(request);
        }

        public void PutUserRootFile(string portalTag, string userId, string fileName, string contentType, MemoryStream fileData)
        {
            var request = new PutObjectRequest()
            {
                BucketName = this.RootBucket + "/" + portalTag + "/drafts/" + userId,
                Key = fileName,
                ContentType = contentType,
                InputStream = fileData,
                CannedACL = S3CannedACL.PublicRead
            };
            var response = S3Client.PutObject(request);
        }

        public bool BucketExists(string bucketName)
        {
            var request = new ListObjectsRequest();
            request.BucketName = this.RootBucket;
            
            var buckets = S3Client.ListObjects(request);
            var result = buckets.S3Objects.Any(x => x.Key == bucketName + @"/");
            return result;
        }

        public int TryRemoveBucket(string bucketName)
        {
            var result = 0;
            try
            {
                var request = new ListObjectsRequest
                {
                    BucketName = this.RootBucket,
                    Prefix = bucketName + "/",
                    Delimiter = "/",
                    MaxKeys = int.MaxValue
                };

                var keyList = new List<KeyVersion>();

                do
                {
                    ListObjectsResponse response = S3Client.ListObjects(request);

                    // Process response.
                    foreach (var entry in response.S3Objects)
                    {
                        var key = new KeyVersion() { Key = entry.Key, VersionId = null };
                        keyList.Add(key);
                    }

                    foreach (var entry in response.CommonPrefixes)
                    {
                        var request2 = new ListObjectsRequest
                        {
                            BucketName = this.RootBucket,
                            Prefix = entry,
                            Delimiter = "/",
                            MaxKeys = int.MaxValue
                        };

                        ListObjectsResponse response2 = S3Client.ListObjects(request2);

                        foreach (var entry2 in response2.S3Objects)
                        {
                            var key = new KeyVersion() { Key = entry2.Key, VersionId = null };
                            keyList.Add(key);
                        }
                    
                    }
                    
                    // If response is truncated, set the marker to get the next 
                    // set of keys.
                    if (response.IsTruncated)
                    {
                        request.Marker = response.NextMarker;
                    }
                    else
                    {
                        request = null;
                    }
                } while (request != null);



                if (keyList.Any())
                {
                    foreach (var keys in keyList.Partition(900))
                    {
                        var response = S3Client.DeleteObjects(new DeleteObjectsRequest() { BucketName = this.RootBucket, Objects = keys.ToList() });
                        result += response.DeletedObjects.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
                    
            return result;
        }

        public bool FileExists(string path)
        {
            var request = new GetObjectMetadataRequest()
            {
                BucketName = this.RootBucket,
                Key = path
            };
            try
            {
                var response = S3Client.GetObjectMetadata(request);
                return (response.HttpStatusCode == HttpStatusCode.OK);
            }
            catch (AmazonS3Exception ex)
            {
                string errorCode = ex.ErrorCode;
                if (!errorCode.Equals("NotFound"))
                {
                    throw;
                } 
                return false;
            }
        }
    }


}