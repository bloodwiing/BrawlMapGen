using AMGBlocks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace BMG.Preset.Old
{
    public class Preset : PresetBase
    {
        // LOADING

        public static Preset LoadPreset(Meta meta)
        {
            if (meta.Format != PresetType.Old)
                throw new ApplicationException("PRESET TYPE loading mismatch");


            string file = Path.Combine(".", "presets", meta.SystemName, meta.Linker.Data);
            string data;


            using (StreamReader reader = new StreamReader(file))
                data = reader.ReadToEnd();


            var instance = JsonConvert.DeserializeObject<Preset>(data, new AMGBlockReader());  // TODO: Converters


            instance.Meta = meta;


            return instance;
        }


        // IMPLEMENTATIONS

        public override TileBase[] Tiles => tiles;

        public override Dictionary<string, BiomeBase> Biomes => _biomeDict.ToDictionary(item => item.Key, item => (BiomeBase)item.Value);
        public override BiomeBase[] BiomeArray => biomes;
        public override BiomeBase DefaultBiome => defaultBiome;

        public override Dictionary<string, GameModeDefinitionBase> GameModes => _gameModeDict.ToDictionary(item => item.Key, item => (GameModeDefinitionBase)item.Value);


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

            foreach (var gameMode in gamemodes)
                _gameModeDict.TryAdd(gameMode.name, gameMode);
        }

        public PresetOptions presetOptions { get; set; }
        public char[] ignoreTiles { get; set; } = new char[0];

        private Dictionary<string, BiomeData> _biomeDict = new Dictionary<string, BiomeData>();
        private Dictionary<string, GameModeData> _gameModeDict = new Dictionary<string, GameModeData>();

        public Tile[] tiles { get; set; }
        public BiomeData[] biomes { get; set; }
        public BiomeData defaultBiome { get; set; }
        public GameModeData[] gamemodes { get; set; }
        public Dictionary<string, TileDefault[]> metadata { get; set; } = new Dictionary<string, TileDefault[]>();

        private Background[] _backgrounds;
        public Background[] backgrounds { get => _backgrounds; set { _backgrounds = value; RegisterParameters(value); } }

        public class Tile : TileBase
        {
            // IMPLEMENTATIONS

            public override string Name => tileName;
            public override char Code => tileCode;
            protected override TileVariantBase[] Variants => tileTypes;


            public string tileName { get; set; }
            public char tileCode { get; set; }
            public TileType[] tileTypes { get; set; }
            public TileLink tileLinks { get; set; }
        }

        public class TileTypeBase : TileAssetBase
        {
            // IMPLEMENTATIONS

            public override string Asset => asset;
            public override Vector2 Offset
            {
                get => new Vector2(-tileParts.left, -tileParts.top);
                set => tileParts = new TileParts() { top = -value.y, left = -value.x };
            }

            public override BMG.EffectBase[] Effects => throw new NotImplementedException();


            public TileParts tileParts { get; set; }
            public string asset { get; set; }
        }

        public class TileType : TileVariantBase
        {
            // IMPLEMENTATIONS

            public override string Asset => asset;
            public override Vector2 Offset
            {
                get => new Vector2(-tileParts.left, -tileParts.top);
                set => tileParts = new TileParts() { top = -value.y, left = -value.x };
            }

            public override int RowLayer => orderHor.GetValueOrDefault(0);
            public override int Layer => order.GetValueOrDefault(0);
            public override TileAssetBase[] Randomizer => randomizer;

            public override BMG.EffectBase[] Effects => throw new NotImplementedException();


            public TileParts tileParts { get; set; }
            public string asset { get; set; }

            public int? orderHor { get; set; }
            public int? order { get; set; }
            public TileTypeBase[] randomizer { get; set; }
        }

        public class TileParts
        {
            public int top { get; set; } = 0;
            public int mid { get; set; } = 1000;
            public int bot { get; set; } = 0;
            public int left { get; set; } = 0;
            public int right { get; set; } = 0;
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
            // IMPLEMENTATIONS

            public override string Name => name;

            public override bool HasBackground => background != null;
            public override string BackgroundName => background.name;
            public override Dictionary<string, object> BackgroundOptions => background.parameters;

            protected override Dictionary<string, int> TileVariants => _tileVariants;


            [OnDeserialized]
            internal void Prepare(StreamingContext context)
            {
                foreach (var def in defaults)
                    _tileVariants.TryAdd(def.tile, def.type);
            }


            private Dictionary<string, int> _tileVariants = new Dictionary<string, int>();


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

        public class GameModeDataVariant : GameModeBase
        {
            // IMPLEMENTATIONS
            
            public override SpecialBase[] SpecialTiles => specialTiles;
            public override Dictionary<string, int> BiomeOverrides => _overrideDict;
            public override ModBase[] MapMods => mapModder;


            [OnDeserialized]
            internal void Prepare(StreamingContext context)
            {
                foreach (var ob in overrideBiome)
                    _overrideDict.TryAdd(ob.tile, ob.type);
            }

            private Dictionary<string, int> _overrideDict = new Dictionary<string, int>();


            public SpecialTile[] specialTiles { get; set; }
            public TileDefault[] overrideBiome { get; set; }
            public MapMod[] mapModder { get; set; }
        }

        public class GameModeData : GameModeDefinitionBase
        {
            // IMPLEMENTATIONS

            public override string Name => name;
            public override SpecialBase[] SpecialTiles => specialTiles;
            public override Dictionary<string, int> BiomeOverrides => _overrideDict;
            public override ModBase[] MapMods => mapModder;
            public override Dictionary<string, GameModeBase> Variants => variants.ToDictionary(item => item.Key, item => (GameModeBase)item.Value);


            [OnDeserialized]
            internal void Prepare(StreamingContext context)
            {
                if (overrideBiome != null)
                    foreach (var ob in overrideBiome)
                        _overrideDict.TryAdd(ob.tile, ob.type);
            }

            private Dictionary<string, int> _overrideDict = new Dictionary<string, int>();


            public string name { get; set; }
            public SpecialTile[] specialTiles { get; set; }
            public TileDefault[] overrideBiome { get; set; }
            public MapMod[] mapModder { get; set; }
            public Dictionary<string, GameModeDataVariant> variants { get; set; } = new Dictionary<string, GameModeDataVariant>();
        }

        public class SpecialTile : GameModeBase.SpecialBase
        {
            // IMPLEMENTATIONS

            public override string Tile => tile;
            public override int Type => type;
            public override string Position => position;
            public override GameModePass Pass => drawOrder == 1 ? GameModePass.BACK : GameModePass.FRONT;


            public string tile { get; set; }
            public int type { get; set; }
            public string position { get; set; }
            public int drawOrder { get; set; }
        }

        public class MapMod : GameModeBase.ModBase
        {
            // IMPLEMENTATIONS

            public override string Tile => tile;
            public override string Position => position;


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
