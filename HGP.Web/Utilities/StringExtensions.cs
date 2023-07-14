using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Linq;
using System.Text;

namespace HGP.Web.Utilities
{

    public static class StringExtensions
    {
        public static string Pluralize(this string str, int n)
        {
            if (n != 1)
                return PluralizationService.CreateService(new CultureInfo("en-US")) 
                .Pluralize(str);
            return str;
        }
        
        public static string AsCsv<T>(this IEnumerable<T> items)
    where T : class
        {
            var csvBuilder = new StringBuilder();
            var properties = typeof(T).GetProperties();
            foreach (T item in items)
            {
                //string line = properties.Select(p => p.GetValue(item, null).ToCsvValue()).ToArray().Join(",");
                string line= string.Join(", ", properties.Select(p => p.GetValue(item, null).ToCsvValue()).ToArray());
                csvBuilder.AppendLine(line);
            }
            return csvBuilder.ToString();
        }

        private static string ToCsvValue<T>(this T item)
        {
            if (item == null)
                return "";

            if (item is string)
            {
                return string.Format("\"{0}\"", item.ToString().Replace("\"", "\\\""));
            }
            double dummy;
            if (double.TryParse(item.ToString(), out dummy))
            {
                return string.Format("{0}", item.ToString());
            }
            return string.Format("\"{0}\"", item.ToString());
        }
        
        public static string StripPunctuation(this string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (!char.IsPunctuation(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        public static string StripWhiteSpace(this string s)
        {
            return new string(s.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        public static string Left(this string s, int len)
        {
            if (string.IsNullOrEmpty(s)){ return s; }
            if (s.Length <= len) { return s; } 
            else { return s.Substring(0, len); }
        }
    } 

}