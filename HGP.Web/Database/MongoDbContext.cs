using System;
using System.Collections;
using System.Threading;
using System.Web;
using HGP.Common.Database;
using HGP.Web.DependencyResolution;

namespace HGP.Web.Database
{
    public static class MongoDbContext
    {
        private static readonly string HTTPCONTEXTKEY;
        private static readonly Hashtable _threads = new Hashtable();


        static MongoDbContext()
        {
            HTTPCONTEXTKEY = "Session.Base.HttpContext.Key." + typeof(IMongoRepository);
        }

        /// <summary>
        /// Returns a database context or creates one if it doesn't exist.
        /// </summary>
        public static IMongoRepository Current
        {
            get
            {
                return GetOrCreateSession();
            }
        }

        /// <summary>
        /// Returns true if a database context is open.
        /// </summary>
        public static bool IsOpen
        {
            get
            {
                var session = GetSession();
                return (session != null);
            }
        }

        #region Private Helpers

        private static IMongoRepository GetOrCreateSession()
        {
            var session = GetSession();
            if (session == null)
            {
                session = IoC.Container.GetInstance<MongoRepository>();

                SaveSession(session);
            }

            return session;
        }

        private static IMongoRepository GetSession()
        {
            if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Items.Contains(HTTPCONTEXTKEY))
                {
                    return (IMongoRepository)HttpContext.Current.Items[HTTPCONTEXTKEY];
                }

                return null;
            }
            else
            {
                Thread thread = Thread.CurrentThread;
                if (string.IsNullOrEmpty(thread.Name))
                {
                    thread.Name = Guid.NewGuid().ToString();
                    return null;
                }
                else
                {
                    lock (_threads.SyncRoot)
                    {
                        return (IMongoRepository)_threads[Thread.CurrentThread.Name];
                    }
                }
            }
        }

        private static void SaveSession(IMongoRepository session)
        {
            if (HttpContext.Current != null)
            {
                HttpContext.Current.Items[HTTPCONTEXTKEY] = session;
            }
            else
            {
                lock (_threads.SyncRoot)
                {
                    _threads[Thread.CurrentThread.Name] = session;
                }
            }
        }

        #endregion
    }

}
