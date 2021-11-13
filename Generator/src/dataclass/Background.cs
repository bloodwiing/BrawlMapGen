using AMGBlocks;

namespace BMG
{
    public abstract class BackgroundBase
    {
        public abstract string Name { get; }

        public abstract IActionBlock Blocks { get; }
        public abstract BlocksParameterBase[] Parameters { get; }


        public AMGBlockFunction Function { get; set; }
    }
}
