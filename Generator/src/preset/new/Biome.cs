using Idle.Serialization;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace BMG.Preset.New
{
    [Serializable]
    public class BiomesRoot : ArrayRootBase<BiomeArray, Biome>
    {
        [IdleProperty("DEFAULT")]
        public Biome DefaultBiome;

        [IdleProperty("BIOME")]
        public override List<Biome> Array { get; protected set; } = new List<Biome>();
    }


    [Serializable]
    public class BiomeArray : ITypeArray<Biome>
    {
        [IdleProperty("BIOME")]
        public Biome[] Data { get; set; } = new Biome[0];
    }


    [Serializable]
    public class Biome : IBiome
    {
        [IdleFlag("NAME")]
        public string Name { get; private set; }

        [IdleShortChildFlag("BACKGROUND")]
        public string Background { get; private set; } = null;

        [IdleProperty("BACKGROUND")]
        public Dictionary<string, object> BackgroundOptions { get; private set; } = new Dictionary<string, object>();

        [IdleProperty("TILES")]
        public Dictionary<string, int> Tiles { get; private set; } = new Dictionary<string, int>();

        public bool HasBackground => Background != null;

        public void ApplyOverride(string tile, int type)
        {
            throw new NotImplementedException();
        }

        public int GetTileVariant(ITile tile)
        {
            throw new NotImplementedException();
        }

        public void ResetOverrides()
        {
            throw new NotImplementedException();
        }

        public Color SolveBackgroundColor()
        {
            throw new NotImplementedException();
        }
    }
}
