using System;

namespace Idle.Serialization.Abstract
{
    public abstract class IdleAbstractFlag : Attribute
    {
        public abstract bool Nameless { get; }
    }
}