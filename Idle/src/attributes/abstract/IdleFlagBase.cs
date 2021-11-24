namespace Idle.Serialization.Abstract
{
    public abstract class IdleFlagBase : IdleAbstractFlag
    {
        public abstract Flag GetFlag(Atom atom);
        public abstract bool TryGetFlag(Atom atom, out Flag flag);
    }
}