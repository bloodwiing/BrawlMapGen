using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using Svg;
using System.IO;
using Newtonsoft.Json;

namespace generator
{
    class Program
    {
        static void Main(string[] args)
        {

            //string[] map = { "........2.2.2........", "..RRR...........RRR..", "..RRR..Y.....Y..RRR..", ".......Y..9..Y.......", ".......YYYYYYY.......", "RRRRRRRRRRRRRRRRRRR..", "NNNNNCRRRRRRRRRCNNN..", "RRRRRRRRRRRRRRRRRRR..", "RRRRRRRRRRRRRRRRRRR..", ".......CNNNNNC.......", ".....................", ".....................", "YYYYY...YYYYYNNNNN...", "....WWWWW............", "....WWWWW............", ".....................", ".....................", ".....................", "............WWWWW....", "............WWWWW....", "...NNNNNYYYYY...YYYYY", ".....................", ".....................", ".......CNNNNNC.......", "..RRRRRRRRRRRRRRRRRRR", "..RRRRRRRRRRRRRRRRRRR", "..NNNCRRRRRRRRRCNNNNN", "..RRRRRRRRRRRRRRRRRRR", ".......YYYYYYY.......", ".......Y..9..Y.......", "..RRR..Y.....Y..RRR..", "..RRR...........RRR..", "........1.1.1........" };
            //string[] map = { "........2.2.2........", "..FFF...........FFF..", "..FFF..Y.....Y..FFF..", ".......Y..9..Y.......", ".......YYYYYYY.......", "FFFFFFFFFFFFFFFFFFF..", "NNNNNCFFFFFFFFFCNNN..", "FFFFFFFFFFFFFFFFFFF..", "FFFFFFFFFFFFFFFFFFF..", ".......CNNNNNC.......", ".....................", ".....................", "YYYYY...YYYYYNNNNN...", "....WWWWW............", "....WWWWW............", ".....................", ".....................", ".....................", "............WWWWW....", "............WWWWW....", "...NNNNNYYYYY...YYYYY", ".....................", ".....................", ".......CNNNNNC.......", "..FFFFFFFFFFFFFFFFFFF", "..FFFFFFFFFFFFFFFFFFF", "..NNNCFFFFFFFFFCNNNNN", "..FFFFFFFFFFFFFFFFFFF", ".......YYYYYYY.......", ".......Y..9..Y.......", "..FFF..Y.....Y..FFF..", "..FFF...........FFF..", "........1.1.1........" };
            //int sizeMultiplier = 20;
            
            if (!File.Exists("options.json"))
            {
                Environment.Exit(0);
            }

            StreamReader r = new StreamReader("options.json");
            string json = r.ReadToEnd();
            var options = JsonConvert.DeserializeObject<Options>(json);

            if (!File.Exists("presets\\" + options.preset + ".json"))
            {
                Environment.Exit(0);
            }

            StreamReader r2 = new StreamReader("presets\\" + options.preset + ".json");
            string json2 = r2.ReadToEnd();
            var tiledata = JsonConvert.DeserializeObject<Tiledata>(json2);

            var map = options.map;
            var sizeMultiplier = options.sizeMultiplier;

            if (map == null)
            {
                Console.WriteLine("Missing map");
            }
            else if (map.Length == 0)
            {
                Console.WriteLine("Missing map");
            }

            int xLength = map[0].Length;
            int yLength = map.Length;

            Console.WriteLine("X:" + xLength + " Y:" + yLength);

            /*Rectangle tileBounds = new Rectangle(x * tileWidth, y * tileHeight, tileWidth, tileHeight);
            Bitmap target = new Bitmap(tileWidth, tileHeight);
            using (Graphics graphics = Graphics.FromImage(target))
            {
                graphics.DrawImage(
                    image,
                    new Rectangle(0, 0, tileWidth, tileHeight),
                    tileBounds,
                    GraphicsUnit.Pixel);
            }

            target.Save(outputFileName, ImageFormat.Jpeg);*/

            Bitmap b = new Bitmap(sizeMultiplier * 2 + sizeMultiplier * xLength, sizeMultiplier * 2 + sizeMultiplier * yLength);
            Graphics g = Graphics.FromImage(b);

            int currentY = 0;
            int currentX = 0;

            string[] color1s = tiledata.biomes[options.biome - 1].color1.Split(',');
            Color color1 = Color.FromArgb(int.Parse(color1s[0].Trim()), int.Parse(color1s[1].Trim()), int.Parse(color1s[2].Trim()));

            string[] color2s = tiledata.biomes[options.biome - 1].color2.Split(',');
            Color color2 = Color.FromArgb(int.Parse(color2s[0].Trim()), int.Parse(color2s[1].Trim()), int.Parse(color2s[2].Trim()));

            foreach (string row in map)
            {

                foreach (char tile in row.ToCharArray())
                {
                    if (currentY % 2 == 0)
                    {
                        if (currentX % 2 == 0)
                        {
                            g.FillRectangle(new SolidBrush(color1), sizeMultiplier + sizeMultiplier * currentX, sizeMultiplier + sizeMultiplier * currentY, sizeMultiplier, sizeMultiplier);
                        }
                        else
                        {
                            g.FillRectangle(new SolidBrush(color2), sizeMultiplier + sizeMultiplier * currentX, sizeMultiplier + sizeMultiplier * currentY, sizeMultiplier, sizeMultiplier);
                        }
                    }
                    else
                    {
                        if (currentX % 2 == 0)
                        {
                            g.FillRectangle(new SolidBrush(color2), sizeMultiplier + sizeMultiplier * currentX, sizeMultiplier + sizeMultiplier * currentY, sizeMultiplier, sizeMultiplier);
                        }
                        else
                        {
                            g.FillRectangle(new SolidBrush(color1), sizeMultiplier + sizeMultiplier * currentX, sizeMultiplier + sizeMultiplier * currentY, sizeMultiplier, sizeMultiplier);
                        }
                    }

                    currentX++;

                }

                currentX = 0;
                currentY++;

            }

            currentY = 0;
            currentX = 0;

            foreach (string row in map)
            {
                List<PriorityTile> priorityTiles = new List<PriorityTile>();

                foreach (char tTile in row.ToCharArray())
                {

                    /*if (tile == 'R')
                    {
                        var i = SvgDocument.Open(tiledata.type[0].options.respawningForest.asset);
                        var iw = (int)Math.Round(i.Width * sizeMultiplier);
                        var ih = (int)Math.Round(i.Height * sizeMultiplier);
                        var ihm = (int)Math.Round((double)tiledata.type[0].options.respawningForest.tileParts.top * sizeMultiplier / 1000);
                        g.DrawImage(i.Draw(iw, ih), sizeMultiplier + sizeMultiplier * currentX, sizeMultiplier + sizeMultiplier * currentY - ihm);
                    }
                    else if (tile == 'Y')
                    {
                        var i = SvgDocument.Open(tiledata.type[0].options.blocking3.asset);
                        var iw = (int)Math.Round(i.Width * sizeMultiplier);
                        var ih = (int)Math.Round(i.Height * sizeMultiplier);
                        var ihm = (int)Math.Round((double)tiledata.type[0].options.blocking3.tileParts.top * sizeMultiplier / 1000);
                        g.DrawImage(i.Draw(iw, ih), sizeMultiplier + sizeMultiplier * currentX, sizeMultiplier + sizeMultiplier * currentY - ihm);
                    }
                    else if (tile == 'N')
                    {
                        var i = SvgDocument.Open(tiledata.type[0].options.fence.asset);
                        var iw = (int)Math.Round(i.Width * sizeMultiplier);
                        var ih = (int)Math.Round(i.Height * sizeMultiplier);
                        var ihm = (int)Math.Round((double)tiledata.type[0].options.fence.tileParts.top * sizeMultiplier / 1000);
                        var iwm = (int)Math.Round((double)tiledata.type[0].options.fence.tileParts.left * sizeMultiplier / 1000);
                        g.DrawImage(i.Draw(iw, ih), sizeMultiplier + sizeMultiplier * currentX - iwm, sizeMultiplier + sizeMultiplier * currentY - ihm);
                    }*/

                    var tile = tTile;

                    foreach (Options.Replace repTile in options.replaceTiles)
                    {
                        if (tile == repTile.from)
                        {
                            tile = repTile.to;
                        }
                    }

                    string NeighborBinary = "";

                    foreach (Tiledata.Tile aTile in tiledata.tiles)
                    {
                        if (aTile.tileCode == tile)
                        {
                            foreach (Tiledata.TileDefault tileDefault in tiledata.biomes[options.biome - 1].defaults)
                            {
                                if (tileDefault.tile == aTile.tileName)
                                {
                                    if (aTile.tileLinks != null)
                                    {
                                        if (currentY != 0 && currentX != 0) { if (map[currentY - 1].ToCharArray()[currentX - 1] == aTile.tileCode) { NeighborBinary = "1"; } else NeighborBinary = "0"; } else NeighborBinary = "0";
                                        if (currentY != 0) { if (map[currentY - 1].ToCharArray()[currentX] == aTile.tileCode) { NeighborBinary = NeighborBinary + "1"; } else NeighborBinary = NeighborBinary + "0"; } else NeighborBinary = NeighborBinary + "0";
                                        if (currentY != 0 && currentX != xLength - 1) { if (map[currentY - 1].ToCharArray()[currentX + 1] == aTile.tileCode) { NeighborBinary = NeighborBinary + "1"; } else NeighborBinary = NeighborBinary + "0"; } else NeighborBinary = NeighborBinary + "0";
                                        if (currentX != 0) { if (map[currentY].ToCharArray()[currentX - 1] == aTile.tileCode) { NeighborBinary = NeighborBinary + "1"; } else NeighborBinary = NeighborBinary + "0"; } else NeighborBinary = NeighborBinary + "0";
                                        if (currentX != xLength - 1) { if (map[currentY].ToCharArray()[currentX + 1] == aTile.tileCode) { NeighborBinary = NeighborBinary + "1"; } else NeighborBinary = NeighborBinary + "0"; } else NeighborBinary = NeighborBinary + "0";
                                        if (currentY != yLength - 1 && currentX != 0) { if (map[currentY + 1].ToCharArray()[currentX - 1] == aTile.tileCode) { NeighborBinary = NeighborBinary + "1"; } else NeighborBinary = NeighborBinary + "0"; } else NeighborBinary = NeighborBinary + "0";
                                        if (currentY != yLength - 1) { if (map[currentY + 1].ToCharArray()[currentX] == aTile.tileCode) { NeighborBinary = NeighborBinary + "1"; } else NeighborBinary = NeighborBinary + "0"; } else NeighborBinary = NeighborBinary + "0";
                                        if (currentY != yLength - 1 && currentX != xLength - 1) { if (map[currentY + 1].ToCharArray()[currentX + 1] == aTile.tileCode) { NeighborBinary = NeighborBinary + "1"; } else NeighborBinary = NeighborBinary + "0"; } else NeighborBinary = NeighborBinary + "0";

                                        List<Tiledata.TileLinkRule> accurateRules = new List<Tiledata.TileLinkRule>();

                                        var nbca = NeighborBinary.ToCharArray();
                                        if (aTile.tileLinks.rules.Length != 0)
                                        {
                                            foreach (var rule in aTile.tileLinks.rules)
                                            {
                                                /*for (int x = 0; x < 8; x++)
                                                {
                                                    if (rule.condition.Contains('!'))
                                                    {
                                                        if (rule.condition.Replace("!", "").ToCharArray()[x] != nbca[x])
                                                        {
                                                            for (int y = 0; y < rule.changeBinary.Length; y++)
                                                            {
                                                                nbca[int.Parse(rule.changeBinary[y].Split('a')[1]) - 1] = rule.changeBinary[y].Split('a')[0].ToCharArray()[0];
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (rule.condition.ToCharArray()[x] == nbca[x])
                                                        {
                                                            for (int y = 0; y < rule.changeBinary.Length; y++)
                                                            {
                                                                nbca[int.Parse(rule.changeBinary[y].Split('a')[1]) - 1] = rule.changeBinary[y].Split('a')[0].ToCharArray()[0];
                                                            }
                                                        }

                                                    }
                                                }*/
                                                int accuracy = 0;
                                                for (int x = 0; x < 8; x++)
                                                {
                                                    if (rule.condition.Contains('!'))
                                                    {
                                                        if (rule.condition.Replace("!", "").ToCharArray()[x] == '*')
                                                        {
                                                            accuracy++;
                                                        }
                                                        else if (rule.condition.Replace("!", "").ToCharArray()[x] != nbca[x])
                                                        {
                                                            accuracy++;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (rule.condition.ToCharArray()[x] == '*')
                                                        {
                                                            accuracy++;
                                                        }
                                                        else if (rule.condition.ToCharArray()[x] == nbca[x])
                                                        {
                                                            accuracy++;
                                                        }
                                                    }
                                                }

                                                if (accuracy == 8)
                                                {
                                                    accurateRules.Add(rule);
                                                }
                                            }
                                        }

                                        var defaultType = aTile.tileTypes[aTile.tileLinks.defaults.tileType - 1];
                                        foreach (Tiledata.TileLinkRule aRule in accurateRules)
                                        {
                                            if (aRule.changeBinary != null)
                                            {
                                                for (int y = 0; y < aRule.changeBinary.Length; y++)
                                                {
                                                    nbca[int.Parse(aRule.changeBinary[y].Split('a')[1]) - 1] = aRule.changeBinary[y].Split('a')[0].ToCharArray()[0];
                                                }
                                            }
                                            /*if (aRule.changeAsset != null)
                                            {
                                                if (aRule.changeAsset.Contains("!"))
                                                {
                                                    Console.WriteLine("changed");
                                                    defaultAsset = aRule.changeAsset.Split('!')[0];
                                                    defaultType = aTile.tileTypes[int.Parse(aRule.changeAsset.Split('!')[1].Split('=')[1]) - 1];
                                                }
                                                else
                                                {
                                                    defaultAsset = aRule.changeAsset;
                                                }
                                            }*/
                                            if (aRule.changeTileType != null)
                                            {
                                                defaultType = aTile.tileTypes[aRule.changeTileType.GetValueOrDefault() - 1];
                                            }
                                        }

                                        var defaultAsset = defaultType.asset;
                                        if (defaultAsset.Contains("!"))
                                        {
                                            Console.WriteLine("changed");
                                            defaultAsset = defaultAsset.Split('!')[0];
                                        }

                                        var fullBinaryFinal = string.Join("", nbca);

                                        if (defaultAsset.Contains("?binary+"))
                                        {
                                            Console.WriteLine("changed");
                                            defaultAsset = defaultAsset.Replace("?binary+", fullBinaryFinal);
                                        }

                                        //Console.WriteLine(fullBinaryFinal);
                                        //Console.WriteLine("assets\\tiles\\" + options.preset + "\\" + aTile.tileTypes[tileDefault.type - 1].assetFolder + "\\" + String.Join("", nbca) + ".svg");

                                        string fols = "";
                                        if (aTile.tileLinks.assetFolder != null)
                                        {
                                            fols = aTile.tileLinks.assetFolder + "\\";
                                        }
                                        var assetst = fullBinaryFinal + ".svg";

                                        if (defaultType.priority != null)
                                        {
                                            //Console.WriteLine(tile + " written at " + currentX);
                                            priorityTiles.Add(new PriorityTile()
                                            {
                                                tileType = defaultType,
                                                xPosition = currentX,
                                                yPosition = currentY
                                            });
                                            continue;
                                        }

                                        Console.WriteLine("assets\\tiles\\" + options.preset + "\\" + fols + defaultAsset);
                                        var li = SvgDocument.Open("assets\\tiles\\" + options.preset + "\\" + fols + defaultAsset);
                                        var liw = (int)Math.Round(li.Width * sizeMultiplier);
                                        var lih = (int)Math.Round(li.Height * sizeMultiplier);
                                        var lihm = (int)Math.Round((double)defaultType.tileParts.top * sizeMultiplier / 1000);
                                        var liwm = (int)Math.Round((double)defaultType.tileParts.left * sizeMultiplier / 1000);

                                        //Console.WriteLine(li + " @ " + liw + " @ " + lih + " @ " + lihm + " @ " + liwm);

                                        g.DrawImage(li.Draw(liw, lih), sizeMultiplier + sizeMultiplier * currentX - liwm, sizeMultiplier + sizeMultiplier * currentY - lihm);
                                        continue;
                                    }

                                    if (aTile.tileTypes[tileDefault.type - 1].priority != null)
                                    {
                                        //Console.WriteLine(tile + " written at " + currentX);
                                        priorityTiles.Add(new PriorityTile()
                                        {
                                            tileType = aTile.tileTypes[tileDefault.type - 1],
                                            xPosition = currentX,
                                            yPosition = currentY
                                        });
                                        continue;
                                    }

                                    var i = SvgDocument.Open("assets\\tiles\\" + options.preset + "\\" + aTile.tileTypes[tileDefault.type - 1].asset);
                                    var iw = (int)Math.Round(i.Width * sizeMultiplier);
                                    var ih = (int)Math.Round(i.Height * sizeMultiplier);
                                    var ihm = (int)Math.Round((double)aTile.tileTypes[tileDefault.type - 1].tileParts.top * sizeMultiplier / 1000);
                                    var iwm = (int)Math.Round((double)aTile.tileTypes[tileDefault.type - 1].tileParts.left * sizeMultiplier / 1000);
                                    g.DrawImage(i.Draw(iw, ih), sizeMultiplier + sizeMultiplier * currentX - iwm, sizeMultiplier + sizeMultiplier * currentY - ihm);
                                    continue;
                                }
                            }
                        }
                    }

                    Console.WriteLine("Y:" + (currentY + 1) + " X:" + (currentX + 1) + " TileCode:" + tile);
                    //Console.WriteLine(tile + " skipped at " + currentX);
                    currentX++;

                }

                int currentPriority = 0;
                ++currentPriority;
                foreach (var pTile in priorityTiles)
                {
                    if (pTile == null)
                    {
                        continue;
                    }
                    Console.WriteLine(pTile.tileType.priority.GetValueOrDefault());
                    if (pTile.tileType.priority.GetValueOrDefault() != currentPriority)
                    {
                        continue;
                    }
                    var i = SvgDocument.Open("assets\\tiles\\" + options.preset + "\\" + pTile.tileType.asset);
                    var iw = (int)Math.Round(i.Width * sizeMultiplier);
                    var ih = (int)Math.Round(i.Height * sizeMultiplier);
                    var ihm = (int)Math.Round((double)pTile.tileType.tileParts.top * sizeMultiplier / 1000);
                    var iwm = (int)Math.Round((double)pTile.tileType.tileParts.left * sizeMultiplier / 1000);
                    g.DrawImage(i.Draw(iw, ih), sizeMultiplier + sizeMultiplier * pTile.xPosition - iwm, sizeMultiplier + sizeMultiplier * pTile.yPosition - ihm);
                }

                currentX = 0;
                currentY++;

            }

            b.Save("Output.png", ImageFormat.Png);
                
            Console.ReadKey(true);

        }

    }
}
