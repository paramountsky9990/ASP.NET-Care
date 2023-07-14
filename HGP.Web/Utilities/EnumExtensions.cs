using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Utilities
{
    public static class EnumExtensions
    {
        public static T ToEnum<T>(this string value, bool ignoreCase = true)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }
    }
}