using Idle.Serialization.Abstract;
using System;

namespace Idle.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple=false)]
    public class IdleShortFlagAttribute : IdleFlagBase
    {
        public readonly int position;

        public override bool Nameless => true;

        public IdleShortFlagAttribute(int position = 0)
        {
            this.position = position;
        }

        public override Flag GetFlag(Atom atom) => atom.GetFlag(position);
        public override bool TryGetFlag(Atom atom, out Flag flag) => atom.TryGetFlag(position, out flag);
    }
}