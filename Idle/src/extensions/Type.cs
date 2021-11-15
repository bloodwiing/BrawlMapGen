using System;
using System.Collections.Generic;
using System.Linq;

namespace Idle.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsEnumerator(this Type type)
        {
            return type
                .GetInterfaces()
                .Any(
                    x => x.IsGenericType &&
                    x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        public static bool IsList(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }
    }
}
