using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace BMG
{
    public class Cache
    {
        readonly static Dictionary<int, CacheInstance> instances = new Dictionary<int, CacheInstance>();

        public static CacheInstance GetInstance(OptionsBase options, int scale)
        {
            if (!instances.TryGetValue(scale, out CacheInstance instance))
            {
                instance = new CacheInstance(options, scale);
                instances.Add(scale, instance);
            }
            return instance;
        }
    }

    public class CacheInstance
    {
        private readonly Dictionary<string, TileImage> tileImages = new Dictionary<string, TileImage>();

        private Random rnd;

        private readonly OptionsBase options;
        private readonly int scale;

        public CacheInstance(OptionsBase options, int scale)
        {
            this.options = options;
            this.scale = scale;

            if (options.RandomizerSeed != null)
                rnd = new Random(options.RandomizerSeed.Value);
            else
                rnd = new Random();
        }

        public void SetRandomSeed(int seed)
        {
            rnd = new Random(seed);
        }

        public TileImage GetTileImage(TileVariantBase tile)
        {
            string asset = tile.Asset;
            TileAssetBase final = tile;

            if (tile.Randomizer != null && options.HasRandomizer)
            {
                final = tile.Randomizer[rnd.Next(tile.Randomizer.Length)];
                asset = final.Asset;

                if (final.Offset == null)
                    final.Offset = tile.Offset;
            }

            if (tileImages.ContainsKey(asset))
                return tileImages[asset];

            var instance = new TileImage(options, scale, final);
            tileImages.Add(asset, instance);
            return instance;
        }
    }

    public class TileImage
    {
        public Bitmap renderedImage;
        public string imageName;
        public int imageWidth;
        public int imageHeight;

        public TileImage(OptionsBase options, int scale, TileAssetBase tile)
        {
            string path = string.Format("./assets/tiles/{0}/{1}", options.Preset, tile.Asset);

            if (!File.Exists(path))
                throw new FileNotFoundException("File " + path + " does not exist.");

            imageName = tile.Asset;

            Logger.LogAAL(Logger.AALDirection.In, "./assets/tiles/" + options.Preset + "/" + tile.Asset);
            var document = SvgDocument.Open(path);

            imageWidth = (int)Math.Round((double)document.Width * scale);
            imageHeight = (int)Math.Round((double)document.Height * scale);

            renderedImage = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);

            var renderer = SvgRenderer.FromImage(renderedImage);
            renderer.ScaleTransform(scale, scale);
            document.Draw(renderer);
        }

    }
}
