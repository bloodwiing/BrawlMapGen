using Idle.Serialization.Abstract;
using System;

namespace Idle.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class IdleFlagAttribute : IdleFlagBase
    {
        public readonly string name;

        public override bool Nameless => false;

        public IdleFlagAttribute(string name)
        {
            this.name = name;
        }

        public override Flag GetFlag(Atom atom) => atom.GetFlag(name);
        public override bool TryGetFlag(Atom atom, out Flag flag) => atom.TryGetFlag(name, out flag);
    }
}