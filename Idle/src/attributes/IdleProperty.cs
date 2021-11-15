using System;

namespace Idle.Serialization
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class IdlePropertyAttribute : Attribute
    {
        public readonly string label;

        public IdlePropertyAttribute(string label = null)
        {
            this.label = label;
        }
    }
}