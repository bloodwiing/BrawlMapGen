using Svg;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace BMG.Cache
{
    public class TileCache
    {
        public Bitmap renderedImage;
        public string imageName;
        public int imageWidth;
        public int imageHeight;

        public TileCache(OptionsBase options, int scale, GraphicLayer tile)
        {
            // GET SVG PATH [TEMP]

            string path = string.Format("./presets/{0}/assets/{1}", options.Preset, tile.Asset);

            if (!File.Exists(path))
                throw new FileNotFoundException("File " + path + " does not exist.");


            // SAVE CACHED IMAGE NAME

            imageName = tile.Asset;


            // LOAD SVG

            Logger.LogAAL(Logger.AALDirection.In, path);
            var document = SvgDocument.Open(path);


            // PREDICT SIZE

            imageWidth = (int)Math.Round((double)document.Width * scale);
            imageHeight = (int)Math.Round((double)document.Height * scale);


            // PREPARE CANVAS

            renderedImage = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);


            // RENDER SCALED SVG TO CANVAS

            var renderer = SvgRenderer.FromImage(renderedImage);
            renderer.ScaleTransform(scale, scale);
            document.Draw(renderer);
        }

    }
}
