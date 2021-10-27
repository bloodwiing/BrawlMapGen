using BMG.State;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BMG
{
    public abstract class MapBase
    {
        public abstract string RawName { get; set; }
        public string GetName()
        {
            return Utils.StringVariables(
                RawName,
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
                    { "INDEX", AMGState.map.index }
                }
                );
        }


        public abstract string[] Data { get; set; }
        public abstract object Biome { get; }

        public Rectangle Size => new Rectangle(Data.Length, Data[0].Length);
        public void AutoCrop(char[] tiles)
        {
            int t, b, l = Size.width, r = 0;
            string line;

            for (t = 0; t < Size.height; t++)
            {
                line = Data[t];
                foreach (char c in tiles)
                    line = line.Replace(c.ToString(), string.Empty);
                if (line != string.Empty)
                    break;
            }

            for (b = Size.height - 1; b >= 0; b--)
            {
                line = Data[b];
                foreach (char c in tiles)
                    line = line.Replace(c.ToString(), string.Empty);
                if (line != string.Empty)
                    break;
            }

            for (int e = t; e <= b; e++)
            {
                line = Data[e];
                if (line.Length - line.TrimStart(tiles).Length < l)
                    l = line.Length - line.TrimStart(tiles).Length;
                if (line.Length - line.TrimEnd(tiles).Length < r)
                    r = line.Length - line.TrimEnd(tiles).Length;
            }

            if (t != 0 || b != Size.height - 1 || l != 0 || r != 0)
            {
                Data = Data
                    .Skip(t)
                    .Take(b - t + 1)
                    .Select(item => item.Substring(l, item.Length - l - r))
                    .ToArray();

                Logger.LogSpacer();
                Logger.LogSetup("Auto Cropped Map:", false);
                Logger.LogSetup(string.Format(
                    "  {0} Top\n  {1} Bottom\n  {2} Left\n  {3} Right",
                    t, Size.height - b - 1, l, r
                ), false);
            }
        }


        public Range GetLayerRange(Preset.PresetBase preset, BiomeBase biome)
        {
            Range range = null;


            // CHECK ALL LAYERS

            foreach (TileBase tile in preset.Tiles)
            {
                TileVariantBase variant = tile.GetVariant(biome);

                if (range == null)
                    range = new Range(variant.Layer);
                else
                    range.Insert(variant.Layer);

                range.Insert(variant.RowLayer);
            }


            return range;
        }


        public abstract int Scale { get; set; }
        public abstract char[] VoidTiles { get; }

        public abstract Dictionary<string, int> BiomeOverrides { get; }

        public void ApplyOverrides(BiomeBase biome)
        {
            foreach ((string tile, int type) in BiomeOverrides)
                biome.ApplyOverride(tile, type);
        }

        public abstract Margin Margin { get; }

        public abstract string GameMode { get; }

        public abstract int? GenerationSeed { get; }
    }
}
