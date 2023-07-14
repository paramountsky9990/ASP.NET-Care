#region Using

using System;

#endregion

namespace HGP.Web.Extensions
{
    public static class Lazy
    {
        public static Lazy<T> From<T>(Func<T> loader)
        {
            return new Lazy<T>(loader);
        }
    }
}