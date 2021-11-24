using Idle.Serialization.Abstract;
using System;

namespace Idle.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class IdleChildFlagAttribute : IdleChildFlagBase
    {
        public readonly string name;

        public override bool Nameless => false;

        public IdleChildFlagAttribute(string label, string name)
        {
            this.label = label;
            this.name = name;
        }

        public IdleChildFlagAttribute(string label, int index, string name) : this(label, name)
        {
            this.index = index;
        }

        public override Flag GetFlag(Item item) => item.GetFlag(name);
        public override bool TryGetFlag(Item item, out Flag flag) => item.TryGetFlag(name, out flag);
    }
}