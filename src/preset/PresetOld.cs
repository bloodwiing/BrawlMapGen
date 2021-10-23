using AMGBlocks;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BMG
{
    public class PresetOld : PresetBase
    {
        // IMPLEMENTATIONS
        public override Dictionary<string, BiomeBase> Biomes => _biomeDict.ToDictionary(item => item.Key, item => (BiomeBase)item.Value);
        public override BiomeBase[] BiomeArray => biomes;
        public override BiomeBase DefaultBiome => defaultBiome;


        [OnDeserialized]
        internal void Prepare(StreamingContext context)
        {
            foreach (var biome in biomes)
            {
                List<TileDefault> update = biome.defaults.ToList();

                foreach (var def in defaultBiome.defaults)
                {
                    bool ready = false;
                    foreach (var setting in biome.defaults)
                        if (def.tile == setting.tile)
                        {
                            ready = true; break;
                        }

                    if (!ready)
                        update.Add(def);
                }

                biome.defaults = update.ToArray();

                _biomeDict.TryAdd(biome.name, biome);
            }
        }

        public PresetOptions presetOptions { get; set; }
        public char[] ignoreTiles { get; set; } = new char[0];

        private Dictionary<string, BiomeData> _biomeDict = new Dictionary<string, BiomeData>();

        public Tile[] tiles { get; set; }
        public BiomeData[] biomes { get; set; }
        public BiomeData defaultBiome { get; set; }
        public Gamemode[] gamemodes { get; set; }
        public Dictionary<string, TileDefault[]> metadata { get; set; } = new Dictionary<string, TileDefault[]>();

        private Background[] _backgrounds;
        public Background[] backgrounds { get => _backgrounds; set { _backgrounds = value; RegisterParameters(value); } }

        public class Tile
        {
            public string tileName { get; set; }
            public char tileCode { get; set; }
            public TileType[] tileTypes { get; set; }
            public TileLink tileLinks { get; set; }
        }

        public class TileTypeBase
        {
            public TileParts tileParts { get; set; }
            public string asset { get; set; }
        }

        public class TileType : TileTypeBase
        {
            public string color { get; set; }
            public bool detailed { get; set; }
            public bool visible { get; set; }
            public string other { get; set; }
            public int? orderHor { get; set; }
            public int? order { get; set; }
            public bool tileTransitions { get; set; } = false;
            public TileTypeBase[] randomizer { get; set; }
        }

        public class TileParts
        {
            public int top { get; set; }
            public int mid { get; set; }
            public int bot { get; set; }
            public int left { get; set; }
            public int right { get; set; }
        }

        public class TileDefault
        {
            public string tile { get; set; }
            public int type { get; set; }
        }

        public class BackgroundChoice
        {
            public string name { get; set; }
            public Dictionary<string, object> parameters { get; set; }
        }

        public class BiomeData : BiomeBase
        {
            public override string Name => name;

            public override bool HasBackground => background != null;
            public override string BackgroundName => background.name;
            public override Dictionary<string, object> BackgroundOptions => background.parameters;

            public string name { get; set; }
            public BackgroundChoice background { get; set; }
            public TileDefault[] defaults { get; set; }
        }

        public class TileLink
        {
            public TileLinkRule[] rules { get; set; }
            public bool multipleConditionsCouldApply { get; set; }
            public TileLinkDefault defaults { get; set; }
            public EdgeCase edgeCase { get; set; }
            public string assetFolder { get; set; }
        }

        public class TileLinkRule
        {
            public string condition { get; set; }
            public int? requiredBiome { get; set; }
            public string[] changeBinary { get; set; }
            public int? changeTileType { get; set; }
            public string changeAsset { get; set; }
            public string changeFolder { get; set; }
        }

        public class TileLinkDefault
        {
            public int tileType { get; set; }
            public string asset { get; set; }
        }

        public class GamemodeBase
        {
            public SpecialTile[] specialTiles { get; set; }
            public TileDefault[] overrideBiome { get; set; }
            public MapMod[] mapModder { get; set; }
        }

        public class Gamemode : GamemodeBase
        {
            public string name { get; set; }
            public Dictionary<string, GamemodeBase> variants { get; set; }
        }

        public class SpecialTile
        {
            public string tile { get; set; }
            public int type { get; set; }
            public string position { get; set; }
            public int drawOrder { get; set; }
        }

        public class MapMod
        {
            public string tile { get; set; }
            public string position { get; set; }
        }

        public enum EdgeCase
        {
            different, copies, mirror
        }

        public class PresetOptions
        {
            public int tileTransitionSize { get; set; }
        }

        public class AMGBlocksParameter : BlocksParameterBase
        {
            // IMPLEMENTATIONS

            public override string Name => name;
            public override string Type => type;
            public override object Default { get => @default; set => @default = value; }


            public string name { get; set; }
            public string type { get; set; }

            private object _default;
            public object @default
            {
                get => _default;
                set
                {
                    if (value is JObject jObject)
                    {
                        if (jObject.ContainsKey("r") && jObject.ContainsKey("g") && jObject.ContainsKey("b"))
                            jObject.Add("type", "COLOR");
                        else
                            throw new ApplicationException("Unknown default parameter type for " + name);
                    }
                    _default = value;
                }
            }
        }

        public class Background : BackgroundBase
        {
            // IMPLEMENTATIONS

            public override string Name => name;
            public override IActionBlock Blocks => blocks;
            public override BlocksParameterBase[] Parameters => parameters;


            public string name { get; set; }
            public IActionBlock blocks { get; set; }
            public AMGBlocksParameter[] parameters { get; set; } = new AMGBlocksParameter[0];
        }

    }
}
