using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace BMG
{
    public class SavedImages
    {
        private readonly Dictionary<string, TileImage> tileImages = new Dictionary<string, TileImage>();

        private Random rnd = new Random();

        private readonly Options1 optionsObject;
        private readonly int sizeMultiplier;

        public SavedImages(Options1 optionsObject, int sizeMultiplier)
        {
            this.optionsObject = optionsObject;
            this.sizeMultiplier = sizeMultiplier;
        }

        public void SetRandomSeed(int seed)
        {
            rnd = new Random(seed);
        }

        public TileImage GetTileImage(Tiledata.TileType tile)
        {
            string asset = tile.asset;
            Tiledata.TileTypeBase final = tile;

            if (tile.randomizer != null && optionsObject.randomizers.enabled)
            {
                final = tile.randomizer[rnd.Next(tile.randomizer.Length)];
                asset = final.asset;

                if (final.tileParts == null)
                    final.tileParts = tile.tileParts;
            }

            if (tileImages.ContainsKey(asset))
                return tileImages[asset];

            var instance = new TileImage(optionsObject, sizeMultiplier, final);
            tileImages.Add(asset, instance);
            return instance;
        }

        public class TileImage
        {
            public Bitmap renderedImage;
            public int imageOffsetTop;
            public int imageOffsetLeft;
            public string imageName;
            public int imageWidth;
            public int imageHeight;

            public TileImage(Options1 optionsObject, int sizeMultiplier, Tiledata.TileTypeBase tile)
            {
                string path = string.Format("./assets/tiles/{0}/{1}", optionsObject.preset, tile.asset);

                if (!File.Exists(path))
                    throw new FileNotFoundException("File " + path + " does not exist.");

                imageName = tile.asset;

                imageOffsetTop = (int)Math.Round((double)tile.tileParts.top * sizeMultiplier / 1000);
                imageOffsetLeft = (int)Math.Round((double)tile.tileParts.left * sizeMultiplier / 1000);

                Logger.LogAAL(Logger.AALDirection.In, "./assets/tiles/" + optionsObject.preset + "/" + tile.asset);
                var document = SvgDocument.Open(path);

                imageWidth = (int)Math.Round((double)document.Width * sizeMultiplier);
                imageHeight = (int)Math.Round((double)document.Height * sizeMultiplier);

                renderedImage = document.Draw(imageWidth, imageHeight);
            }

        }

    }

}
