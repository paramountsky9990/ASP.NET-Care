using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using HGP.Web.Database;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HGP.Web.Models
{
    public interface IMediaFile
    {
        string FileName { get; set; }
        byte[] FileData { get; set; }
        byte[] ThumbnailData { get; set; }
        bool IsImage { get; set; }
        string ContentType { get; set; }
        short SortOrder { get; set; }
    }

    public class MediaFile
    {
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
        public byte[] ThumbnailData { get; set; }
        public byte[] LargeThumbnailData { get; set; }
        public bool IsImage { get; set; }
        public string ContentType { get; set; }
        public short SortOrder { get; set; }
    }

    public class MediaFileDto
    {
        public string FileName { get; set; }
        public bool IsImage { get; set; }
        public string ContentType { get; set; }
        public short SortOrder { get; set; }
        public string ImageUrl { get; set; }
        public string ThumbnailUrl { get; set; }

    }

    //public class IdentityDraftImg
    //{
    //    [BsonRepresentation(BsonType.ObjectId)]
    //    public string Id { get; set; }
    //    public string Path_1 { get; set; }
    //    public string DraftId { get; set; }
    //}
}