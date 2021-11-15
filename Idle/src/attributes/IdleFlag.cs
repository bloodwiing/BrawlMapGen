using System;

namespace Idle.Serialization
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class IdleFlagAttribute : IdleFlagBase
    {
        public readonly string name;

        public override bool Nameless => false;

        public IdleFlagAttribute(string label, string name)
        {
            this.label = label;
            this.name = name;
        }

        public IdleFlagAttribute(string label, int index, string name) : this(label, name)
        {
            this.index = index;
        }

        public override Flag GetFlag(Item item) => item.GetFlag(name);
        public override bool TryGetFlag(Item item, out Flag flag) => item.TryGetFlag(name, out flag);
    }
}