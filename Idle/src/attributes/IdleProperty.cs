using System;

namespace Idle.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class IdlePropertyAttribute : Attribute
    {
        public readonly string label;

        public IdlePropertyAttribute(string label)
        {
            this.label = label;
        }
    }
}