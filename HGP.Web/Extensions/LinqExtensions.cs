using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Extensions
{
    public static class LinqExtensions
    {
        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> items,
                                                              int partitionSize)
        {
            int i = 0;
            return items.GroupBy(x => i++ / partitionSize).ToArray();
        }

        public static bool IsIn<T>(this T source, params T[] values)
        {
            return values.Contains(source);
        }
    }
}