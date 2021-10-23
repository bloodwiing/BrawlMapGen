using AMGBlocks;
using BMG.State;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BMG
{
    public class Renderer
    {
        Graphics graphics;
        Bitmap bitmap;

        MapBase map;

        OptionsBase options;
        PresetBase preset;

        public int CanvasWidth => bitmap.Width;
        public int CanvasHeight => bitmap.Height;


        public Renderer(MapBase map, OptionsBase options, PresetBase preset)
        {
            Margin margin = map.Margin;
            Rectangle size = map.Size;

            bitmap = new Bitmap(
                (int)Math.Round(map.Scale * (margin.left + margin.right + size.width)),  // Width
                (int)Math.Round(map.Scale * (margin.top + margin.bottom + size.height))  // Height
                );
            graphics = Graphics.FromImage(bitmap);

            this.map = map;

            this.options = options;
            this.preset = preset;
        }


        //private Tiledata.TileType GetRealAsset(Tiledata.Tile tile, int type, OptionsOld optionsObject, string defaultAsset, int? overrideType = null)
        //{
        //    Tiledata.TileType asset = tile.tileTypes[overrideType.GetValueOrDefault(type)];
        //    asset.asset = defaultAsset;

        //    if (optionsObject.assetSwitchers != null)
        //        foreach (OptionsOld.AssetSwitcher switcher in optionsObject.assetSwitchers)
        //        {
        //            if (tile.tileName == switcher.find.tile && type == switcher.find.type)
        //                foreach (Tiledata.Tile dTile in t.tiles)
        //                    if (dTile.tileName == switcher.replace.tile)
        //                        asset = dTile.tileTypes[switcher.replace.type];

        //        }

        //    return asset;
        //}


        //private Tiledata.TileType GetRealAsset(OrderedTile tile, int type, OptionsOld optionsObject, string defaultAsset)
        //{
        //    return GetRealAsset(new Tiledata.Tile() { tileName = tile.tileName, tileTypes = new Tiledata.TileType[] { tile.tileTypeData } }, type, optionsObject, defaultAsset, 0);
        //}


        public void DrawGameMode(GameModeBase gameMode, GameModePass pass)
        {
            if (gameMode == null)
                return;

            if (gameMode.SpecialTiles != null)
                DrawGameModeSpecials(gameMode, pass);
        }


        private void DrawGameModeSpecials(GameModeBase gameMode, GameModePass pass)
        {
            foreach (var special in gameMode.SpecialTiles)
            {
                // CHECK PASS

                if (special.Pass == pass)
                    continue;


                // GET DATA

                Vector2 position = Utils.ParsePosition(special.Position);
                TileVariantBase tile = preset.GetTileVariant(special.Tile, special.Type);


                // DRAW TILE

                DrawTile(tile, options, position);
                AMGState.drawer.DrawnTile();
                //Logger.LogTile(new TileActionTypes(1, 0, 0, 0, 1), oTile, ysLoc, xsLoc, yLength, xLength, Logger.TileEvent.tileDraw);
            }
        }


        public void DrawTile(TileVariantBase tile, OptionsBase options, Vector2 position)
        {
            // GET ASSET

            TileVariantBase asset = tile;


            // CALCULATE ASSET OFFSET

            Vector2 offset = asset.Offset.Clone();
            offset *= map.Scale;
            offset /= 1000;


            // CALCULATE REAL CANVAS POSITION

            position += (map.Margin.left, map.Margin.top);
            position *= map.Scale;
            position += offset;



            // GET CACHED IMAGE

            var image = Cache.GetInstance(options, map.Scale).GetTileImage(asset);


            // RENDER

            graphics.DrawImage(
                image.renderedImage,
                position
                );
        }


        //public void DrawTile(Tiledata.Tile tile, int type, OptionsOld optionsObject, int sizeMultiplier, int currentX, int currentY, SavedImages imageMemory, float[] borderSize) // Drawing a tile (normal)
        //{
        //    var real = GetRealAsset(tile, type, optionsObject, tile.tileTypes[type].asset);

        //    int offTop = (int)Math.Round((double)real.tileParts.top * sizeMultiplier / 1000);
        //    int offLeft = (int)Math.Round((double)real.tileParts.left * sizeMultiplier / 1000);

        //    var ti = imageMemory.GetTileImage(real);

        //    graphics.DrawImage(ti.renderedImage, (int)Math.Round(sizeMultiplier * (currentX + borderSize[2])) - offLeft, (int)Math.Round(sizeMultiplier * (currentY + borderSize[0])) - offTop);
        //    return;
        //}


        //public void DrawSelectedTile(OrderedTile tile, OptionsOld optionsObject, int sizeMultiplier, SavedImages imageMemory, float[] borderSize) // Drawing a tile (with saved coordinates and a pre-selected type)
        //{
        //    var real = GetRealAsset(tile, tile.tileType, optionsObject, tile.tileTypeData.asset);

        //    int offTop = (int)Math.Round((double)real.tileParts.top * sizeMultiplier / 1000);
        //    int offLeft = (int)Math.Round((double)real.tileParts.left * sizeMultiplier / 1000);

        //    var ti = imageMemory.GetTileImage(real);

        //    graphics.DrawImage(ti.renderedImage, (int)Math.Round(sizeMultiplier * (tile.xPosition + borderSize[2])) - offLeft, (int)Math.Round(sizeMultiplier * (tile.yPosition + borderSize[0])) - offTop);
        //    return;
        //}


        public void ColorBackground(MapBase map) // Filling in background colors
        {
            // SETUP

            Margin margin = map.Margin;


            // RESET CURSOR

            AMGState.drawer.ResetCursor();


            // GET BACKGROUND OR DEFAULT

            BiomeBase bg = preset.GetBiome(map);

            if (!bg.HasBackground)
                bg = preset.DefaultBiome;


            // RUN ON EVERY TILE

            object result;

            while (!AMGState.map.drawn)
            {
                // SKIP IF VOID

                if (map.VoidTiles.Contains(AMGState.ReadAtCursor()))
                {
                    AMGState.MoveCursor();
                    continue;
                }


                // RUN BLOCKS

                if (bg.BackgroundOptions != null && bg.BackgroundOptions.Count > 0)  // Run with parameters
                    result = preset.BackgroundManagerInstance.RunFunction(bg.BackgroundName, bg.BackgroundOptions);
                else  // Run without parameters
                    result = preset.BackgroundManagerInstance.RunFunction(bg.BackgroundName);


                // FILL WITH RESULT COLOR

                if (result is ColorData color)
                {
                    graphics.FillRectangle(
                        new SolidBrush(Color.FromArgb(color.r, color.g, color.b)),  // Draw color
                        (int)Math.Round(map.Scale * (AMGState.drawer.cursor.x + margin.left)),  // Pos X
                        (int)Math.Round(map.Scale * (AMGState.drawer.cursor.y + margin.top)),  // Pos Y
                        map.Scale, map.Scale  // Size
                    );
                    AMGState.MoveCursor();  // Continue
                }
                else
                    throw new ApplicationException("Expected a Color output from background blocks, but received " + result.GetType().ToString());
            }
        }


        public void ExportImage() // Saving the generated image
        {
            // MAKE DIR IF DOESN'T EXIST

            if (!Directory.Exists(options.Output))
                Directory.CreateDirectory(options.Output);


            // SAVE TO FILE

            string fileName = map.GetName() + ".png";

            if (Regex.IsMatch(fileName, "\\S:"))
                bitmap.Save(fileName, ImageFormat.Png);
            else
                bitmap.Save(options.Output + "/" + fileName, ImageFormat.Png);


            // DISPOSE

            bitmap.Dispose();
            graphics.Dispose();
        }


        //public static string tileLinks(string[] map, int currentX, int currentY, Tiledata.Tile tileObject, OptionsOld.Replace[] replaces)
        //{
        //    string binary = "";
        //    string neighbors;
        //    if (currentY == 0 && currentY == map.Length - 1) // One line
        //    {
        //        if (currentX == 0 && currentX == map[0].Length - 1)
        //            neighbors = "%%%%%%%%"; // One column
        //        else if (currentX == 0)
        //            neighbors = "%%%% %%%"; // Left
        //        else if (currentX == map[0].Length - 1)
        //            neighbors = "%%% %%%%"; // Right
        //        else
        //            neighbors = "%%%  %%%"; // Middle
        //    }
        //    else if (currentY == 0) // Top
        //    {
        //        if (currentX == 0 && currentX == map[0].Length - 1)
        //            neighbors = "%%%%%% %"; // One column
        //        else if (currentX == 0)
        //            neighbors = "%%%% %  "; // Left
        //        else if (currentX == map[0].Length - 1)
        //            neighbors = "%%% %  %"; // Right
        //        else
        //            neighbors = "%%%     "; // Middle
        //    }
        //    else if (currentY == map.Length - 1) // Bottom
        //    {
        //        if (currentX == 0 && currentX == map[0].Length - 1)
        //            neighbors = "% %%%%%%"; // One column
        //        else if (currentX == 0)
        //            neighbors = "%  % %%%"; // Left
        //        else if (currentX == map[0].Length - 1)
        //            neighbors = "  % %%%%"; // Right
        //        else
        //            neighbors = "     %%%"; // Middle
        //    }
        //    else // Middle
        //    {
        //        if (currentX == 0 && currentX == map[0].Length - 1)
        //            neighbors = "% %%%% %"; // One column
        //        else if (currentX == 0)
        //            neighbors = "%  % %  "; // Left
        //        else if (currentX == map[0].Length - 1)
        //            neighbors = "  % %  %"; // Right
        //        else
        //            neighbors = "        "; // Middle
        //    }

        //    switch (tileObject.tileLinks.edgeCase)
        //    {
        //        case Tiledata.EdgeCase.different: // edges are filled with non-equal tiles
        //            for (int x = 0; x < neighbors.Length; x++)
        //            {
        //                if (neighbors[x] == '%')
        //                    binary += '0'; // edgeCase
        //                else
        //                    binary += checkNeighboringTile(map, currentX, currentY, tileObject, replaces, x); // check tile in map
        //            }
        //            break;
        //        case Tiledata.EdgeCase.copies: // edges are filled with equal tiles
        //            for (int x = 0; x < neighbors.Length; x++)
        //            {
        //                if (neighbors[x] == '%')
        //                    binary += '1'; // edgeCase
        //                else
        //                    binary += checkNeighboringTile(map, currentX, currentY, tileObject, replaces, x); // check tile in map
        //            }
        //            break;
        //        case Tiledata.EdgeCase.mirror: // edges are extended
        //            for (int x = 0; x < neighbors.Length; x++)
        //            {
        //                if (neighbors[x] == '%')
        //                    if (x % 2 == 1)
        //                        binary += '1'; // edgeCase (Edge adjacent edge tiles will always be equal when extending)
        //                    else
        //                    {
        //                        if (hasAdjacentEqualTiles(map, currentX - 1, currentY - 1, tileObject))
        //                            binary += '1';
        //                        else if (hasAdjacentEqualTiles(map, currentX - 1, currentY + 1, tileObject))
        //                            binary += '1';
        //                        else if (hasAdjacentEqualTiles(map, currentX + 1, currentY - 1, tileObject))
        //                            binary += '1';
        //                        else if (hasAdjacentEqualTiles(map, currentX + 1, currentY + 1, tileObject))
        //                            binary += '1';
        //                        else
        //                            binary += '0';
        //                    }
        //                // binary = binary + checkNeighboringTile(map, currentX, currentY, tileObject, 7 - x); // edgeCase (check opposite tile to extend)
        //                else
        //                    binary += checkNeighboringTile(map, currentX, currentY, tileObject, replaces, x); // check tile in map
        //            }
        //            break;
        //    }

        //    return binary;
        //}


        //public static char checkNeighboringTile(string[] map, int currentX, int currentY, Tiledata.Tile tile, OptionsOld.Replace[] replaces, int neighbor)
        //{
        //    switch (neighbor)
        //    {
        //        case 0:
        //            if (CNTFilter(map[currentY - 1].ToCharArray()[currentX - 1], replaces) == tile.tileCode)
        //                return '1';
        //            else
        //                return '0';
        //        case 1:
        //            if (CNTFilter(map[currentY - 1].ToCharArray()[currentX], replaces) == tile.tileCode)
        //                return '1';
        //            else
        //                return '0';
        //        case 2:
        //            if (CNTFilter(map[currentY - 1].ToCharArray()[currentX + 1], replaces) == tile.tileCode)
        //                return '1';
        //            else
        //                return '0';
        //        case 3:
        //            if (CNTFilter(map[currentY].ToCharArray()[currentX - 1], replaces) == tile.tileCode)
        //                return '1';
        //            else
        //                return '0';
        //        case 4:
        //            if (CNTFilter(map[currentY].ToCharArray()[currentX + 1], replaces) == tile.tileCode)
        //                return '1';
        //            else
        //                return '0';
        //        case 5:
        //            if (CNTFilter(map[currentY + 1].ToCharArray()[currentX - 1], replaces) == tile.tileCode)
        //                return '1';
        //            else
        //                return '0';
        //        case 6:
        //            if (CNTFilter(map[currentY + 1].ToCharArray()[currentX], replaces) == tile.tileCode)
        //                return '1';
        //            else
        //                return '0';
        //        case 7:
        //            if (CNTFilter(map[currentY + 1].ToCharArray()[currentX + 1], replaces) == tile.tileCode)
        //                return '1';
        //            else
        //                return '0';
        //        default:
        //            return '0';
        //    }
        //}


        //private static char CNTFilter(char original, OptionsOld.Replace[] replaces)
        //{
        //    foreach (var r in replaces)
        //        if (original == r.from)
        //            return r.to;
        //    return original;
        //}


        //public static bool hasAdjacentEqualTiles(string[] map, int x, int y, Tiledata.Tile tileObject)
        //{
        //    if (y < 0) // Top edge
        //    {
        //        if (x < 0) // Left corner
        //        {
        //            if (map[y + 1].ToCharArray()[x + 1] == tileObject.tileCode)
        //                return true;
        //            else return false;
        //        }
        //        else if (x > map[0].Length - 1) // Right corner
        //        {
        //            if (map[y + 1].ToCharArray()[x - 1] == tileObject.tileCode)
        //                return true;
        //            else return false;
        //        }
        //        else // Middle
        //        {
        //            if (x != map[0].Length - 1)
        //                if (map[y + 1].ToCharArray()[x + 1] == tileObject.tileCode)
        //                    return true;
        //                else return false;
        //            else if (x != 0)
        //                if (map[y + 1].ToCharArray()[x - 1] == tileObject.tileCode)
        //                    return true;
        //                else return false;
        //            else return false;
        //        }
        //    }
        //    else if (y > map.Length - 1) // Bottom edge
        //    {
        //        if (x < 0) // Left corner
        //        {
        //            if (map[y - 1].ToCharArray()[x + 1] == tileObject.tileCode)
        //                return true;
        //            else return false;
        //        }
        //        else if (x > map[0].Length - 1) // Right corner
        //        {
        //            if (map[y - 1].ToCharArray()[x - 1] == tileObject.tileCode)
        //                return true;
        //            else return false;
        //        }
        //        else // Middle
        //        {
        //            if (x != map[0].Length - 1)
        //                if (map[y - 1].ToCharArray()[x + 1] == tileObject.tileCode)
        //                    return true;
        //                else return false;
        //            else if (x != 0)
        //                if (map[y - 1].ToCharArray()[x - 1] == tileObject.tileCode)
        //                    return true;
        //                else return false;
        //            else return false;
        //        }
        //    }
        //    else // -
        //    {
        //        if (x < 0) // Left edge
        //        {
        //            if (y != 0)
        //                if (map[y - 1].ToCharArray()[x + 1] == tileObject.tileCode)
        //                    return true;
        //                else return false;
        //            else if (y != map.Length - 1)
        //                if (map[y + 1].ToCharArray()[x + 1] == tileObject.tileCode)
        //                    return true;
        //                else return false;
        //            else return false;
        //        }
        //        else if (x > map[0].Length - 1) // Right edge
        //        {
        //            if (y != 0)
        //                if (map[y - 1].ToCharArray()[x - 1] == tileObject.tileCode)
        //                    return true;
        //                else return false;
        //            else if (y != map.Length - 1)
        //                if (map[y + 1].ToCharArray()[x - 1] == tileObject.tileCode)
        //                    return true;
        //                else return false;
        //            else return false;
        //        }
        //        else // -
        //        {
        //            return false;
        //        }
        //    }

        //}
    }
}
