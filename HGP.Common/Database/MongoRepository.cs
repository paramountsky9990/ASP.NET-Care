using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace HGP.Common.Database
{
    public interface ITextSearchSortable
    {
       double? TextMatchScore { get; set; }
    }

    public class MongoRepository : IMongoRepository
    {
        private MongoDatabase db = null;
        private readonly MongoServer provider = null;
        private readonly string databaseName; // todo: Move connection string to config file
        public bool AllowDatabaseDrop { get; set; } // Dropping the current database requires this this be set to true


        public MongoRepository()
        {

        }

        public MongoRepository(string connectionString, string dbName)
        {
            this.provider = new MongoClient(connectionString).GetServer();

            //this.databaseName = new MongoUrl(connectionString).DatabaseName;
            this.databaseName = dbName;

            //this.SetProfilingLevel(ProfilingLevel.Slow); // todo: Turn off profiling

            this.AllowDatabaseDrop = false;

            // Map global convention to all BSON serialization
            var cp = new ConventionPack();
            cp.Add(new IgnoreExtraElementsConvention(true));
            ConventionRegistry.Register("ApplicationConventions", cp, t => true);
        }

        public IEnumerable<T> TextSearch<T>(string text, string portalId) where T : ITextSearchSortable
        {
            var collection = DataBase.GetCollection<T>(typeof(T).Name + "s");
            var cursor = collection.Find(Query.And(Query.Text(text), Query.EQ("PortalId", portalId)))
                .SetFields(Fields<T>.MetaTextScore(t => t.TextMatchScore))
                .SetSortOrder(SortBy<T>.MetaTextScore(t => t.TextMatchScore)).AsQueryable();
            foreach (var t in cursor)
            {
                // prevent saving the value back into the database
                t.TextMatchScore = null;
                yield return t;
            }
        }

        public MongoCollection<T> GetQuery<T>() where T : class, new()
        {
            var query = DataBase.GetCollection<T>(typeof(T).Name + "s");
            return query;
        }

        private MongoDatabase DataBase
        {
            
            get
            {
                if (db == null)
                {
                    db = provider.GetDatabase(this.databaseName);
                }
                return db;
            }
        }

        public IQueryable<T> All<T>() where T : class, new()
        {
            return this.GetQuery<T>().AsQueryable();
        }

        public IQueryable<T> All<T>(int page, int pageSize) where T : class, new()
        {
            throw new NotImplementedException();
            //return PagingExtensions.Page(All<T>(), page, pageSize);
        }


        public IList<T> GetAll<T>() where T : class, new()
        {
            try
            {
                return GetQuery<T>().FindAll().ToList<T>();
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public IList<T> Find<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class, new()
        {
            return this.GetQuery<T>().AsQueryable<T>().Where(criteria).ToList<T>();
        }

        public T FindAndModify<T>(IMongoQuery query, IMongoSortBy sort, IMongoUpdate update) where T : class, new()
        {
            var result = this.GetQuery<T>().FindAndModify(query, sort, update, true /* return entity */, false /* upsert */).GetModifiedDocumentAs<T>();
            return result;
        }

        public T FindAndModify<T>(FindAndModifyArgs args) where T : class, new()
        {
            var result = this.GetQuery<T>().FindAndModify(args);
           
            return result.GetModifiedDocumentAs<T>();
        }
        
        public T Single<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class, new()
        {
            return this.GetQuery<T>().AsQueryable<T>().SingleOrDefault(criteria);
        }

        public T First<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class, new()
        {
            return this.GetQuery<T>().AsQueryable<T>().FirstOrDefault(criteria);
        }

        public void Add<T>(T entity) where T : class, new()
        {
            this.GetQuery<T>().Save(entity);
        }

        public bool Exists<TEntity>(object key) where TEntity : class, new()
        {
            var collection = GetQuery<TEntity>();
            var query = new QueryDocument("_id", BsonValue.Create(key));
            var entity = collection.FindOneAs<TEntity>(query);
            return (entity != null);
        }

        public void Add<T>(IEnumerable<T> items) where T : class, new()
        {
            foreach (T item in items)
            {
                Add(item);
            }
        }

        public void Delete<T>(object key) where T : class, new()
        {
            var collection = GetQuery<T>();
            var query = new QueryDocument("_id", BsonValue.Create(key));
            collection.Remove(query);
        }

        public void Delete<T>(IEnumerable<T> entites) where T : class, new()
        {
            foreach (T item in entites)
            {
                var doc = item.ToBsonDocument();
                this.Delete<T>(doc["_id"]);
            }
        }

        public void Delete<T>(System.Linq.Expressions.Expression<Func<T, bool>> expression)
            where T : class, new()
        {
            var items = All<T>().Where(expression);
            foreach (T item in items)
            {
                var doc = item.ToBsonDocument();
                this.Delete<T>(doc["_id"]);
            }
        }

        public void Update<T>(T entity) where T : class, new()
        {
            this.GetQuery<T>().Save(entity);
        }

        public void DropDatabase()
        {
            if (this.AllowDatabaseDrop)
            {
                this.DataBase.Drop();
                this.AllowDatabaseDrop = false;
            }
            else
            {
                throw new Exception("Attempt to drop database without setting AllowDatabaseDrop first");
            }
        }

        public void SetProfilingLevel(ProfilingLevel level)
        {
            this.DataBase.SetProfilingLevel(level);
        }
        
        public long Count<T>(System.Linq.Expressions.Expression<Func<T, bool>> criteria) where T : class, new()
        {
            return this.GetQuery<T>().AsQueryable<T>().Count(criteria);
        }
        
        public void Dispose()
        {
            provider.Disconnect();
        }

    }

}
