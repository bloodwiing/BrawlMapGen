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
        private Dictionary<string, TileImage> tileImages = new Dictionary<string, TileImage>();

        private readonly Options1 optionsObject;
        private readonly int sizeMultiplier;
        private readonly Program.Logger logger;

        public SavedImages(Options1 optionsObject, int sizeMultiplier, Program.Logger logger)
        {
            this.optionsObject = optionsObject;
            this.sizeMultiplier = sizeMultiplier;
            this.logger = logger;
        }

        public TileImage GetTileImage(Tiledata.TileType asset)
        {
            if (tileImages.ContainsKey(asset.asset))
                return tileImages[asset.asset];

            var instance = new TileImage(optionsObject, sizeMultiplier, logger, asset);
            tileImages.Add(asset.asset, instance);
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

            public TileImage(Options1 optionsObject, int sizeMultiplier, Program.Logger logger, Tiledata.TileType asset)
            {
                string path = string.Format("./assets/tiles/{0}/{1}", optionsObject.preset, asset.asset);

                if (!File.Exists(path))
                    throw new FileNotFoundException("File " + path + " does not exist.");

                imageName = asset.asset;

                imageOffsetTop = (int)Math.Round((double)asset.tileParts.top * sizeMultiplier / 1000);
                imageOffsetLeft = (int)Math.Round((double)asset.tileParts.left * sizeMultiplier / 1000);

                logger.LogAAL(Program.Logger.AALDirection.In, "./assets/tiles/" + optionsObject.preset + "/" + asset.asset);
                var document = SvgDocument.Open(path);

                imageWidth = (int)Math.Round((double)document.Width * sizeMultiplier);
                imageHeight = (int)Math.Round((double)document.Height * sizeMultiplier);

                renderedImage = document.Draw(imageWidth, imageHeight);
            }

        }

    }

}
