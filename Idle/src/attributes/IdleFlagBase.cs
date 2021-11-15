using System;

namespace Idle.Serialization
{
    public abstract class IdleFlagBase : Attribute
    {
        public string label { get; protected set; }
        public int index { get; protected set; } = 0;

        public abstract bool Nameless { get; }

        public abstract Flag GetFlag(Item item);
        public abstract bool TryGetFlag(Item item, out Flag flag);
    }
}