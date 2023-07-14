using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HGP.Common.Database
{
    public interface IMongoObjectBase
    {
        string Id { get; set; }

        DateTime CreatedDate { get; set; }
        DateTime UpdatedDate { get; set; }
        string CreatedBy { get; set; }
        string UpdatedBy { get; set; }

        bool IsNew { get; set; }
    }

    public class MongoObjectBase : IMongoObjectBase
    {
        [BsonId]
        public string Id { get; set; }

        private bool isEntityNew;

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        public MongoObjectBase()
        {
            this.Id = ObjectId.GenerateNewId().ToString();
            this.isEntityNew = true;
            this.CreatedDate = this.UpdatedDate = DateTime.UtcNow;

            // todo: Grab CreatedBy and UpdatedBy from the current user/thread
        }

        [BsonIgnore]
        public bool IsNew
        {
            get
            {
                return this.isEntityNew;
            }
            set
            {
                isEntityNew = value;
                if (isEntityNew)
                {
                    this.Id = ObjectId.GenerateNewId().ToString();
                    this.CreatedDate = this.UpdatedDate = DateTime.UtcNow;
                }
            }
        }
    }
}
