using System;

namespace Idle.Extensions
{
    class EnumExtensions
    {
        public static object ParseOrNull(Type enumType, object value)
        {
            switch (value)
            {
                case string s:
                    if (Enum.TryParse(enumType, s, out object result))
                        return result;
                    return null;

                case int i:
                    return Convert.ChangeType(i, enumType);

                case long l:
                    return Convert.ChangeType(l, enumType);

            }
            return Convert.ChangeType(0, enumType);
        }
    }
}
