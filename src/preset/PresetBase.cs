using System.Collections.Generic;
using System;
using AMGBlocks;

namespace BMG
{
    public abstract class PresetBase
    {
        // META

        public PresetMeta Meta { get; protected set; }


        // TILES

        public abstract TileBase[] Tiles { get; }

        public TileBase GetTile(string name)
        {
            foreach (var tile in Tiles)
                if (tile.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return tile;
            return null;
        }

        public TileBase GetTile(char code)
        {
            foreach (var tile in Tiles)
                if (tile.Code.Equals(code))
                    return tile;
            return null;
        }

        public TileVariantBase GetTileVariant(string name, int variant)
        {
            TileBase tile = GetTile(name);
            return tile.GetVariant(variant);
        }

        public TileVariantBase GetTileVariant(char code, int variant)
        {
            TileBase tile = GetTile(code);
            return tile.GetVariant(variant);
        }


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
            throw new ApplicationException($"BIOME needs to be an INT or STRING, not {key.GetType().ToString().ToUpper()}");
        }

        public abstract BiomeBase DefaultBiome { get; }


        // GAME MODES

        public abstract Dictionary<string, GameModeDefinitionBase> GameModes { get; }

        public GameModeBase GetGameMode(MapBase map, BiomeBase biome)
        {
            if (map.GameMode == null)
                return null;

            if (!GameModes.TryGetValue(map.GameMode, out GameModeDefinitionBase gameMode))
                return null;

            if (gameMode.Variants.TryGetValue(biome.Name, out GameModeBase variant))
                return variant;

            return gameMode;
        }


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
}
