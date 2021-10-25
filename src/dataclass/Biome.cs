using System.Collections.Generic;

namespace BMG
{
    public abstract class BiomeBase
    {
        public abstract string Name { get; }

        public abstract bool HasBackground { get; }
        public abstract string BackgroundName { get; }
        public abstract Dictionary<string, object> BackgroundOptions { get; }


        protected abstract Dictionary<string, int> TileVariants { get; }
        private Dictionary<string, int> Overrides;


        public void ResetOverrides()
        {
            Overrides = new Dictionary<string, int>();
        }

        public void ApplyOverride(string tile, int type)
        {
            Overrides[tile] = type;
        }


        public int GetTileVariant(string tile)
        {
            if (Overrides.TryGetValue(tile, out int type))
                return type;
            return TileVariants.GetValueOrDefault(tile, 0);
        }
    }
}
