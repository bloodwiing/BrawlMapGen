using BMG.Cache;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;

namespace BMG
{
    public class Renderer
    {
        Graphics graphics;
        Bitmap bitmap;

        public IMap map { get; private set; }

        OptionsBase options;
        IPreset preset;

        public int CanvasWidth => bitmap.Width;
        public int CanvasHeight => bitmap.Height;


        public Renderer(IMap map, OptionsBase options, IPreset preset)
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


        public void DrawGameMode(IGame gameMode, GameModePass pass)
        {
            if (gameMode == null)
                return;

            if (gameMode.SpecialTiles != null)
                DrawGameModeSpecials(gameMode, pass);
        }


        private void DrawGameModeSpecials(IGame gameMode, GameModePass pass)
        {
            gameMode.SpecialTiles.Reset();

            while (gameMode.SpecialTiles.MoveNext())
            {
                var graphic = gameMode.SpecialTiles.Current;


                // CHECK PASS

                if (graphic.Pass != pass)
                    continue;


                // DRAW TILE

                DrawGraphic(graphic, Vector2.Zero);
                AMGState.drawer.DrawnTile();
                //Logger.LogTile(new TileActionTypes(1, 0, 0, 0, 1), oTile, ysLoc, xsLoc, yLength, xLength, Logger.TileEvent.tileDraw);
            }
        }


        public void RenderMap(IBiome biome, Range renderRange)
        {
            // RENDER EACH LAYER

            foreach (int layer in renderRange)
                RenderMapLayer(biome, layer, renderRange);
        }


        public void RenderMapLayer(IBiome biome, int layer, Range renderRange)
        {
            // RESET CURSOR

            AMGState.ResetState();


            // RUN ON WHOLE MAP

            while (!AMGState.map.drawn)
            {
                // RENDER EVERY ROW RANGE TIMES

                foreach (int rowLayer in renderRange)
                    RenderRow(biome, layer, rowLayer);


                // CONTINUE ROWS

                AMGState.MoveVerCursor();
            }
        }


        private void RenderRow(IBiome biome, int layer, int rowLayer)
        {
            // RESET STATE FOR ROW

            AMGState.ResetRowState();


            while (!AMGState.drawer.rowDrawn)
            {
                // SKIP IF VOID

                if (map.IsVoid(AMGState.ReadAtCursor()))
                {
                    AMGState.MoveHorCursor();
                    continue;
                }


                // GET TILE IF EXISTS

                ITile tile = preset.GetTile(AMGState.ReadAtCursor());

                if (tile == null)
                {
                    AMGState.MoveHorCursor();
                    continue;
                }


                // GET TILE GRAPHIC

                if (!preset.MakeTileGraphic(biome, tile, out Graphic graphic))
                {
                    // IF GRAPHIC FAILED

                    AMGState.MoveHorCursor();
                    continue;
                }


                // SKIP IF NOT MEANT FOR LAYER

                if (graphic.ZIndex != layer)
                {
                    AMGState.MoveHorCursor();
                    continue;
                }

                if (graphic.HIndex != rowLayer)
                {
                    AMGState.MoveHorCursor();
                    continue;
                }


                // DRAW TILE

                DrawGraphic(graphic, AMGState.drawer.cursor);
                AMGState.MoveHorCursor();
            }
        }


        public void DrawGraphic(Graphic graphic, Vector2 position)
        {
            // GET CACHE INSTANCE

            var cache = CacheManager.GetInstance(options, map.Scale);


            // CALCULATE REAL CANVAS POSITION

            if (graphic.Position != null)
                position = graphic.Position.Value;

            position += (map.Margin.left, map.Margin.top);
            position *= map.Scale;


            // DRAW EVERY GRAPHIC LAYER
            
            foreach (var layer in graphic.Layers)
            {
                // GET CACHED IMAGE

                var image = cache.GetImage(layer);


                // CALCULATE ASSET OFFSET

                Vector2 offset = layer.Offset.Clone();
                offset *= map.Scale;
                offset /= 1000;


                // RENDER

                graphics.DrawImage(
                    image.renderedImage,
                    position + offset
                    );

            }
        }


        public void ColorBackground(IMap map) // Filling in background colors
        {
            // SETUP

            Margin margin = map.Margin;


            // RESET CURSOR

            AMGState.ResetState();


            // GET BACKGROUND OR DEFAULT

            IBiome biome = preset.GetBiome(map);


            // RUN ON EVERY TILE

            if (!biome.HasBackground)
                biome = preset.DefaultBiome;


            // RUN ON EVERY TILE

            while (!AMGState.map.drawn)
            {
                // SKIP IF VOID

                if (map.IsVoid(AMGState.ReadAtCursor()))
                {
                    AMGState.MoveCursor();
                    continue;
                }


                // RUN BLOCKS

                Color color = biome.SolveBackgroundColor();


                // FILL WITH RESULT COLOR

                graphics.FillRectangle(
                    new SolidBrush(color),  // Draw color
                    (int)Math.Round(map.Scale * (AMGState.drawer.cursor.x + margin.left)),  // Pos X
                    (int)Math.Round(map.Scale * (AMGState.drawer.cursor.y + margin.top)),  // Pos Y
                    map.Scale, map.Scale  // Size
                );
                AMGState.MoveCursor();  // Continue
                
            }

            /*
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
            */
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
