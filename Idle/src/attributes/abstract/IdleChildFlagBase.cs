namespace Idle.Serialization.Abstract
{
    public abstract class IdleChildFlagBase : IdleAbstractFlag
    {
        public string label { get; protected set; }
        public int index { get; protected set; } = 0;

        public abstract Flag GetFlag(Item item);
        public abstract bool TryGetFlag(Item item, out Flag flag);
    }
}