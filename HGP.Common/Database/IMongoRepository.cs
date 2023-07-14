using System.Collections.Generic;
using MongoDB.Driver;

namespace HGP.Common.Database
{
    public interface IMongoRepository : IRepository
    {
        bool AllowDatabaseDrop { get; set; }

        // todo: Should be moved private
        MongoCollection<T> GetQuery<T>() where T : class, new();

        T FindAndModify<T>(IMongoQuery query, IMongoSortBy sort, IMongoUpdate update) where T : class, new();
        T FindAndModify<T>(FindAndModifyArgs args) where T : class, new();

        void SetProfilingLevel(ProfilingLevel level);
        IEnumerable<T> TextSearch<T>(string text, string portalId) where T : ITextSearchSortable;

    }
}
