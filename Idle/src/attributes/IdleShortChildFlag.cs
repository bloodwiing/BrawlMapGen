using Idle.Serialization.Abstract;
using System;

namespace Idle.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple=false)]
    public class IdleShortChildFlagAttribute : IdleChildFlagBase
    {
        public readonly int position;

        public override bool Nameless => true;

        public IdleShortChildFlagAttribute(string label, int position = 0)
        {
            this.label = label;
            this.position = position;
        }

        public IdleShortChildFlagAttribute(string label, int index, int position = 0) : this(label, position)
        {
            this.index = index;
        }

        public override Flag GetFlag(Item item) => item.GetFlag(position);
        public override bool TryGetFlag(Item item, out Flag flag) => item.TryGetFlag(position, out flag);
    }
}