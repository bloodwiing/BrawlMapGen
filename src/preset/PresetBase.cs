using System.Collections.Generic;
using System;
using AMGBlocks;

namespace BMG
{
    public abstract class PresetBase
    {
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


    public abstract class TileAssetBase
    {
        public abstract Vector2 Offset { get; set; }
        public abstract string Asset { get; }
    }


    public abstract class TileVariantBase : TileAssetBase
    {
        public abstract int? RowLayer { get; }
        public abstract int? Layer { get; }

        public abstract TileAssetBase[] Randomizer { get; }
    }


    public abstract class TileBase
    {
        public abstract string Name { get; }
        public abstract char Code { get; }

        public abstract TileVariantBase[] Variants { get; }

        public TileVariantBase GetVariant(int variant)
        {
            if (variant - 1 <= Variants.Length)
                return Variants[variant];
            return Variants[0];
        }
    }


    public abstract class BiomeBase
    {
        public abstract string Name { get; }

        public abstract bool HasBackground { get; }
        public abstract string BackgroundName { get; }
        public abstract Dictionary<string, object> BackgroundOptions { get; }
    }


    public abstract class GameModeBase
    {
        public abstract class SpecialBase
        {
            public abstract string Tile { get; }
            public abstract int Type { get; }
            public abstract string Position { get; }
            public abstract GameModePass Pass { get; }
        }

        public abstract class ModBase
        {
            public abstract string Tile { get; }
            public abstract string Position { get; }
        }

        public abstract SpecialBase[] SpecialTiles { get; }
        public abstract Dictionary<string, int> BiomeOverrides { get; }
        public abstract ModBase[] MapMods { get; }
    }


    public abstract class GameModeDefinitionBase : GameModeBase
    {
        public abstract string Name { get; }

        public abstract Dictionary<string, GameModeBase> Variants { get; }
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
