using System.Collections.Generic;

namespace BMG
{
    public interface IGame
    {
        void ApplyToState(IPreset preset);
        void ApplyOverrides(IBiome biome);

        bool HasSpecialTiles { get; }
        IEnumerator<GameGraphic> SpecialTiles { get; }
    }
}
