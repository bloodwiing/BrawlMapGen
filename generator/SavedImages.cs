using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace generator
{
    public class SavedImages
    {
        public List<TileImage> tileImages = new List<TileImage>();
        public int listForSizeMultiplier;

        public SavedImages(Options optionsObject, Tiledata.Tile[] tiles, int sizeMultiplier, Program.Voice voice)
        {
            foreach (string file in Directory.GetFiles("assets\\tiles\\" + optionsObject.preset + "\\"))
            {
                foreach (Tiledata.Tile tile in tiles)
                {
                    for (int type = 0; type < tile.tileTypes.Length; type++)
                    {
                        if (file.Split('\\').Last() == tile.tileTypes[type].asset)
                        {
                            tileImages.Add(new TileImage()
                            {
                                imageName = file.Split(new string[] { optionsObject.preset + "\\" }, StringSplitOptions.None)[1],
                                imageOffsetTop = (int)Math.Round((double)tile.tileTypes[type].tileParts.top * sizeMultiplier / 1000),
                                imageOffsetLeft = (int)Math.Round((double)tile.tileTypes[type].tileParts.left * sizeMultiplier / 1000)
                            });
                            tileImages.Last().imageWidth = (int)Math.Round((double)SvgDocument.Open(file).Width * sizeMultiplier);
                            tileImages.Last().imageHeight = (int)Math.Round((double)SvgDocument.Open(file).Height * sizeMultiplier);
                            tileImages.Last().renderedImage = SvgDocument.Open(file).Draw(tileImages.Last().imageWidth, tileImages.Last().imageHeight);
                            voice.Speak("[ AAL ] READ << assets\\tiles\\" + optionsObject.preset + "\\" + tileImages.Last().imageName, Program.ActionType.aal);
                            break;
                        }
                    }
                }
            }
            foreach (string folder in Directory.GetDirectories("assets\\tiles\\" + optionsObject.preset + "\\"))
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    foreach (Tiledata.Tile tile in tiles)
                    {
                        for (int type = 0; type < tile.tileTypes.Length; type++)
                        {
                            if (tile.tileLinks != null)
                                if (folder.Split('\\').Last() == tile.tileLinks.assetFolder)
                                {
                                    tileImages.Add(new TileImage()
                                    {
                                        imageName = file.Split(new string[] { optionsObject.preset + "\\" }, StringSplitOptions.None)[1],
                                        imageOffsetTop = (int)Math.Round((double)tile.tileTypes[type].tileParts.top * sizeMultiplier / 1000),
                                        imageOffsetLeft = (int)Math.Round((double)tile.tileTypes[type].tileParts.left * sizeMultiplier / 1000)
                                    });
                                    tileImages.Last().imageWidth = (int)Math.Round((double)SvgDocument.Open(file).Width * sizeMultiplier);
                                    tileImages.Last().imageHeight = (int)Math.Round((double)SvgDocument.Open(file).Height * sizeMultiplier);
                                    tileImages.Last().renderedImage = SvgDocument.Open(file).Draw(tileImages.Last().imageWidth, tileImages.Last().imageHeight);
                                    break;
                                }
                        }
                    }
                }
            }
            listForSizeMultiplier = sizeMultiplier;
        }

        public class TileImage
        {
            public Bitmap renderedImage;
            public int imageOffsetTop;
            public int imageOffsetLeft;
            public string imageName;
            public int imageWidth;
            public int imageHeight;
        }

    }

}
