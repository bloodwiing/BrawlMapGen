namespace BMG
{
    public abstract class BlocksParameterBase
    {
        public abstract string Name { get; }

        public abstract string Type { get; }
        public abstract object Default { get; set; }
    }
}
