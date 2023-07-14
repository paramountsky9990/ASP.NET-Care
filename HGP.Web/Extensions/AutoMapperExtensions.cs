#region Using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;

#endregion

namespace HGP.Web.Extensions
{
    public static class AutoMapperExtensions
    {
        public static IList<TDestination> ToModelList<TDestination>(this IEnumerable query)
        {
            var output = new List<TDestination>();
            var sourceType = query.GetType().GetGenericArguments()[0];
            var destType = output.GetType().GetGenericArguments()[0];

            output.AddRange(from object src in query
                select Mapper.Map(src, sourceType, destType)
                into mySrc
                select Mapper.Map<TDestination>(mySrc));

            return output;
        }

        private static void IgnoreUnmappedProperties(TypeMap map, IMappingExpression expr)
        {
            foreach (string propName in map.GetUnmappedPropertyNames())
            {
                if (map.SourceType.GetProperty(propName) != null)
                {
                    expr.ForSourceMember(propName, opt => opt.Ignore());
                }
                if (map.DestinationType.GetProperty(propName) != null)
                {
                    expr.ForMember(propName, opt => opt.Ignore());
                }
            }
        }

        public static void IgnoreUnmapped(this IProfileExpression profile)
        {
            profile.ForAllMaps(IgnoreUnmappedProperties);
        }

        public static void IgnoreUnmapped(this IProfileExpression profile, Func<TypeMap, bool> filter)
        {
            profile.ForAllMaps((map, expr) =>
            {
                if (filter(map))
                {
                    IgnoreUnmappedProperties(map, expr);
                }
            });
        }

        public static void IgnoreUnmapped(this IProfileExpression profile, Type src, Type dest)
        {
            profile.IgnoreUnmapped((TypeMap map) => map.SourceType == src && map.DestinationType == dest);
        }

        public static void IgnoreUnmapped<TSrc, TDest>(this IProfileExpression profile)
        {
            profile.IgnoreUnmapped(typeof(TSrc), typeof(TDest));
        }
    }
}