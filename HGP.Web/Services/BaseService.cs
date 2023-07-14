using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using HGP.Common.Database;
using HGP.Web.Database;
using HGP.Web.DependencyResolution;
using HGP.Web.Infrastructure;
using HGP.Web.Models;

namespace HGP.Web.Services
{
    public interface IBaseService
    {
        IMongoRepository Repository { get; }
        IWorkContext WorkContext { get; set; }
    }

    public class BaseService<T> : IBaseService where T : MongoObjectBase, new()
    {
        public IMongoRepository Repository { get; private set; }
        public IWorkContext WorkContext { get; set; }

        public BaseService()
            : this(null, null)
        {

        }

        public BaseService(IMongoRepository repository, IWorkContext workContext)
        {
            this.WorkContext = workContext ?? IoC.Container.GetInstance<IWorkContext>(); 
            this.Repository = repository ?? IoC.Container.GetInstance<IMongoRepository>();
        }
        
        public void Save(T entry)
        {
            Contract.Requires(entry != null);

            if (!entry.IsNew)
            {
                entry.UpdatedDate = DateTime.UtcNow;
                // todo: Grab UpdatedBy from the current user/thread
            }

            entry.IsNew = false;

            Repository.Update(entry);
        }

        public T GetById(string id)
        {
            var entity = this.Repository.Single<T>(x => x.Id == id);
            if (entity != null)
                entity.IsNew = false;
            return entity;
        }

        public IList<T> GetList()
        {
            var list = this.Repository.All<T>().ToList();
            return list;
        }

        public void Delete(string id)
        {
            this.Repository.Delete<T>(id);
            return;
        }

        public bool Exists(string id)
        {
            return this.Repository.Exists<T>(id);
        }


    
    }
}
