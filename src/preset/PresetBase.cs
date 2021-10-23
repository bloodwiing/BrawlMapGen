using System.Collections.Generic;
using System;
using AMGBlocks;

namespace BMG
{
    public abstract class PresetBase
    {
        // BIOMES

        public abstract Dictionary<string, BiomeBase> Biomes { get; }
        public abstract BiomeBase[] BiomeArray { get; }

        public BiomeBase GetBiome(int index)
        {
            if (index <= BiomeArray.Length - 1) return BiomeArray[index];
            return DefaultBiome;
        }

        public BiomeBase GetBiome(string name)
        {
            if (Biomes.TryGetValue(name, out BiomeBase biome))
                return biome;
            return DefaultBiome;
        }

        public BiomeBase GetBiome(MapBase map)
        {
            return GetBiome(map.Biome);
        }

        public BiomeBase GetBiome(object key)
        {
            if (key is int @int)
                return GetBiome(@int);
            if (key is long @long)
                return GetBiome(Convert.ToInt32(@long));
            if (key is string @string)
                return GetBiome(@string);
            throw new ApplicationException($"BIOME needs to be an INT or STRING, not {key.GetType()}");
        }

        public abstract BiomeBase DefaultBiome { get; }


        // BACKGROUNDS

        public AMGBlockManager BackgroundManagerInstance = new AMGBlockManager();

        protected void RegisterParameters(BackgroundBase[] bgs)
        {
            foreach (var bg in bgs)
            {
                bg.Function = new AMGBlockFunction(bg.Blocks, bg.Parameters);
                BackgroundManagerInstance.RegisterFunction(bg.Name, bg.Function);
            }
        }
    }


    public abstract class BiomeBase
    {
        public abstract string Name { get; }

        public abstract bool HasBackground { get; }
        public abstract string BackgroundName { get; }
        public abstract Dictionary<string, object> BackgroundOptions { get; }
    }

    
    public abstract class BlocksParameterBase
    {
        public abstract string Name { get; }
        public abstract string Type { get; }
        public abstract object Default { get; set; }
    }


    public abstract class BackgroundBase
    {
        public abstract string Name { get; }
        public abstract IActionBlock Blocks { get; }
        public abstract BlocksParameterBase[] Parameters { get; }


        public AMGBlockFunction Function { get; set; }
    }
}
