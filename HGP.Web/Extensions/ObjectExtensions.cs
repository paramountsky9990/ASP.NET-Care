namespace HGP.Web.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Determines whether the given object value has an instance reference.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool HasValue(this object value)
        {
            return !ReferenceEquals(value, null);
        }
    }
}