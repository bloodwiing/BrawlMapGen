using System.Collections.Generic;

namespace BMG.Abstract
{
    class EmptyGame : IGame
    {
        public bool HasSpecialTiles => throw new System.NotImplementedException();

        public IEnumerator<GameGraphic> SpecialTiles => throw new System.NotImplementedException();

        public void ApplyOverrides(IBiome biome)
        {
            throw new System.NotImplementedException();
        }

        public void ApplyToState(IPreset preset)
        {
            throw new System.NotImplementedException();
        }
    }
}
