using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace HGP.Common.Database
{
    public interface IRepository : IDisposable
    {
        IQueryable<T> All<T>() where T : class, new();
        IQueryable<T> All<T>(int page, int pageSize) where T : class, new();
        IList<T> GetAll<T>() where T : class, new();
        IList<T> Find<T>(Expression<Func<T, bool>> criteria) where T : class, new();
        T Single<T>(Expression<Func<T, bool>> criteria) where T : class, new();
        T First<T>(Expression<Func<T, bool>> criteria) where T : class, new();
        void Add<T>(T entity) where T : class, new();
        bool Exists<TEntity>(object key) where TEntity : class, new();
        void Delete<T>(object key) where T : class, new();
        void Delete<T>(IEnumerable<T> entites) where T : class, new();
        void Delete<T>(Expression<Func<T, bool>> expression)
            where T : class, new();
        void Update<T>(T entity) where T : class, new();
        long Count<T>(Expression<Func<T, bool>> criteria) where T : class, new();

        void DropDatabase();
    }
}
