using BMG.Abstract;
using Idle;
using Idle.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BMG.Preset.New
{
    public partial class Preset : IPreset
    {
        // LOADING

        public static Preset LoadPreset(Meta meta)
        {
            if (meta.Format != PresetType.New)
                throw new ApplicationException("PRESET TYPE loading mismatch");

            string assetFolder = Path.Combine(meta.SystemPath, "Assets");

            Preset instance = new Preset();

            instance.m_tiles = LoadIDLEArray<TilesRoot, Tile>(meta.TilesFile, instance);
            instance.m_biomes = LoadIDLEArray<BiomesRoot, Biome>(meta.BiomesFile, instance)
                .ToDictionary(x => x.Name, x => x);
            instance.m_games = LoadIDLEArray<GamesRoot, Game>(meta.GamesFile, instance)
                .ToDictionary(x => x.Name, x => x);

            instance.m_assets = LoadFolderAssets(assetFolder).ToDictionary(x => x.Name, x => x);

            return instance;
        }

        private static A[] LoadIDLEArray<T, A>(string file, Preset otherData)
            where T : IArrayRoot<A>
        {
            var reader = new IdleReader(file);
            T root = IdleSerializer.Deserialize<T>(reader);
            root.SystemPath = Path.GetDirectoryName(file);

            // Exclusive for biome default
            if (root is BiomesRoot biomesRoot)
                otherData.DefaultBiome = biomesRoot.DefaultBiome;

            return root.Array.ToArray();
        }

        private static IEnumerable<Asset> LoadFolderAssets(string folder)
        {
            // CYCLE OVER FILES IN FOLDER

            foreach (var file in Directory.GetFiles(folder))
            {
                // MUST BE .asset FILE

                if (!string.Equals(Path.GetExtension(file), ".asset", StringComparison.OrdinalIgnoreCase))
                    continue;

                var reader = new IdleReader(file);
                yield return IdleSerializer.Deserialize<Asset>(reader);
            }


            // CYCLE OVER REST OF FOLDERS

            foreach (var child in Directory.GetDirectories(folder))
                foreach (var result in LoadFolderAssets(child))
                    yield return result;
        }

        public IBiome GetBiome(int index)
        {
            if (m_biomes.Count > index)
                return m_biomes.ElementAt(index).Value;
            return DefaultBiome;
        }

        public IBiome GetBiome(string name)
        {
            if (m_biomes.TryGetValue(name, out Biome result))
                return result;
            return DefaultBiome;
        }

        public IBiome GetBiome(IMap map)
        {
            return map.GetBiome(this);
        }

        public ITile GetTile(string name)
        {
            foreach (var tile in m_tiles)
                if (tile.Name == name)
                    return tile;

            throw new ArgumentOutOfRangeException($"Tile of name '{name}' doesn't exist");
        }

        public ITile GetTile(char code)
        {

            foreach (var tile in m_tiles)
                if (tile.Code == code)
                    return tile;

            throw new ArgumentOutOfRangeException($"Tile of code '{code}' doesn't exist");
        }

        public bool MakeTileGraphic(IBiome biome, ITile tile, out Graphic graphic)
        {
            return MakeTileGraphic(tile, biome.GetTileVariant(tile), out graphic);
        }

        public bool MakeTileGraphic(ITile tile, int variant, out Graphic graphic)
        {
            // Using TileVariant get the asset reference  V
            // Change asset reference if assetswitchers  
            // Get Asset object  V
            // Stack layers and effects

            var real = tile as Tile;

            if (variant >= real.variants.Length)
            {
                // warning
                graphic = null;
                return false;
            }

            foreach (var layer in real.variants[variant].Layers)
            {
                if (!m_assets.TryGetValue(layer.Asset, out Asset asset))
                {
                    // warning
                    graphic = null;
                    return false;
                }
            }

            // asset switchers

            throw new NotImplementedException();
        }

        public IGame GetGame(string name, IBiome biome)
        {
            if (!m_games.TryGetValue(name, out Game game))
                return new EmptyGame();

            return game.Fetch(biome);
        }

        public Range GetIndexRange(IMap map)
        {
            IBiome biome = GetBiome(map);


            bool set = false;
            Range range = new Range();


            // CHECK ALL LAYERS

            foreach (var layer in m_tiles.Select(x => x.variants[biome.GetTileVariant(x)]).SelectMany(x => x.Layers))
            {
                if (set)
                    range = new Range(layer.ZIndex);
                else
                    range.Insert(layer.ZIndex);

                range.Insert(layer.HIndex);
            }


            return range;
        }

        private Tile[] m_tiles;
        private Dictionary<string, Biome> m_biomes;
        private Dictionary<string, Game> m_games;
        private Dictionary<string, Asset> m_assets;

        public IBiome DefaultBiome { get; private set; }
    }
}
