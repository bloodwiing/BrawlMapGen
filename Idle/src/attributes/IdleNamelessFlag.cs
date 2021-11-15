using System;

namespace Idle.Serialization
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple=false)]
    public class IdleNamelessFlagAttribute : IdleFlagBase
    {
        public readonly int position;

        public override bool Nameless => true;

        public IdleNamelessFlagAttribute(string label, int position)
        {
            this.label = label;
            this.position = position;
        }

        public IdleNamelessFlagAttribute(string label, int index, int position) : this(label, position)
        {
            this.index = index;
        }

        public override Flag GetFlag(Item item) => item.GetFlag(position);
        public override bool TryGetFlag(Item item, out Flag flag) => item.TryGetFlag(position, out flag);
    }
}