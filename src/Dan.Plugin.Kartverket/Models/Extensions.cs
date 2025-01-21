using Namotion.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Models
{
    public static class Extensions
    {
        public static bool IsNullOrEmpty(this ICollection collection)
        {
            if (collection == null)
                return true;

            return collection.Count < 1;
        }

        public static bool IsNullOrEmpty(this IEnumerable collection)
        {
            if (collection == null)
                return true;

            return !collection.Cast<object>().Any();
        }
    }
}


