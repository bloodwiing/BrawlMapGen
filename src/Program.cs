using System;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace BMG
{
    public class Program
    {
        static void Main(string[] args)
        {

            var fArgs = " " + string.Join(" ", args);

            string oLoc = "options.json";
            string oStr = "";

            if (fArgs.ToLower().Contains(" -f ")) // options.json file location
            {
                oLoc = fArgs.Replace(" -f ", "@").Replace(" -F ", "@").Split('@')[1];
                if (oLoc.Contains(" -"))
                    oLoc = oLoc.Replace(" -", "#").Split('#')[0].Trim();
                else
                    oLoc = oLoc.Trim();
            }

            if (fArgs.ToLower().Contains(" -os ")) // The options, but in string form
            {
                oStr = fArgs.Replace(" -os ", "@").Replace(" -Os ", "@").Replace(" -OS ", "@").Replace(" -oS ", "@").Split('@')[1];
                if (oStr.Contains(" -"))
                    oStr = oStr.Replace(" -", "#").Split('#')[0].Trim();
                else
                    oStr = oStr.Trim();
            }

            if (oStr == "")
            {
                if (!File.Exists(oLoc)) // Check if file exists
                {
                    Logger.LogError("Option file doesn't exist\n  [FileReader] Unable to find file in location \"" + oLoc + "\"");
                    Logger.Save("log.txt");
                    Thread.Sleep(3000);
                    Environment.Exit(1);
                }

                Logger.LogAAL(Logger.AALDirection.In, oLoc);
                StreamReader r = new StreamReader(oLoc);
                oStr = r.ReadToEnd();
                r.Close();
            }

            BMG generator;

            dynamic format = JsonConvert.DeserializeObject(oStr);

            // FORMAT 1
            if ((int)format.format == 1)
                generator = new BMG(JsonConvert.DeserializeObject<OptionsOld>(oStr));

            // INVALID
            else
                throw new ArgumentException("Invalid OPTIONS format");

            generator.Run();
        }
    }
    
    /*
    public class Program
    {
        static void Main(string[] args)
        {

            try
            {
                

                

                    Logger.LogSetup("Drawing map tiles...");
                    if (batchOption.name != "?number?")
                        Logger.LogStatus("Drawing map (\"" + batchOption.name + "\")...");
                    else
                        Logger.LogStatus("Drawing map (#" + AMGState.map.index + ")...");

                    Tiledata.GamemodeBase mapGamemode = null;
                    foreach (var gm in tiledata.gamemodes)
                    {
                        if (gm == null || batchOption.gamemode == null)
                            break;
                        if (gm.name == batchOption.gamemode)
                        {
                            if (gm.variants != null && gm.variants.TryGetValue(mapBiome.name, out var gamemode))
                                mapGamemode = gamemode;
                            else
                                mapGamemode = gm;
                        }
                    }

                    if (mapGamemode != null) // Draw Gamemode Tiles (Before every other tile)
                    {
                        if (mapGamemode.specialTiles != null)
                            foreach (var st in mapGamemode.specialTiles)
                            {
                                if (st.drawOrder == 1)
                                {
                                    foreach (Tiledata.Tile oTile in tiledata.tiles)
                                    {
                                        if (oTile.tileName == st.tile)
                                        {
                                            string xsLoc = st.position.Split(',')[0].Trim().ToLower();
                                            string ysLoc = st.position.Split(',')[1].Trim().ToLower();
                                            if (!int.TryParse(xsLoc, out int xLoc))
                                            {
                                                if (xsLoc == "left" || xsLoc == "l") { xLoc = 0; xsLoc = "L"; }
                                                else if (xsLoc == "mid" || xsLoc == "m") { xLoc = (xLength - 1) / 2; xsLoc = "M"; }
                                                else if (xsLoc == "right" || xsLoc == "r") { xLoc = xLength - 1; xsLoc = "R"; }
                                            }
                                            if (!int.TryParse(ysLoc, out int yLoc))
                                            {
                                                if (ysLoc == "top" || ysLoc == "t") { yLoc = 0; ysLoc = "T"; }
                                                else if (ysLoc == "mid" || ysLoc == "m") { yLoc = (yLength - 1) / 2; ysLoc = "M"; }
                                                else if (ysLoc == "bottom" || ysLoc == "bot" || ysLoc == "b") { yLoc = yLength - 1; ysLoc = "B"; }
                                            }

                                            if (xLoc < 0)
                                            {
                                                xLoc = xLength - (1 + xLoc / -1);
                                                xsLoc = xLoc.ToString();
                                            }
                                            if (yLoc < 0)
                                            {
                                                yLoc = yLength - (1 + yLoc / -1);
                                                ysLoc = yLoc.ToString();
                                            }

                                            tileDrawer.DrawTile(oTile, st.type, options, sizeMultiplier, xLoc, yLoc, selectedTileImageList, border);
                                            AMGState.drawer.DrawnTile();
                                            Logger.LogTile(new TileActionTypes(1, 0, 0, 0, 1), oTile, ysLoc, xsLoc, yLength, xLength, Logger.TileEvent.tileDraw);
                                        }
                                    }
                                }
                            }

                        if (mapGamemode.mapModder != null)
                            foreach (var mod in mapGamemode.mapModder)
                                foreach (Tiledata.Tile oTile in tiledata.tiles)
                                    if (oTile.tileName == mod.tile)
                                    {
                                        string xsLoc = mod.position.Split(',')[0].Trim().ToLower();
                                        string ysLoc = mod.position.Split(',')[1].Trim().ToLower();
                                        if (!int.TryParse(xsLoc, out int xLoc))
                                        {
                                            if (xsLoc == "left" || xsLoc == "l") { xLoc = 0; xsLoc = "L"; }
                                            else if (xsLoc == "mid" || xsLoc == "m") { xLoc = (xLength - 1) / 2; xsLoc = "M"; }
                                            else if (xsLoc == "right" || xsLoc == "r") { xLoc = xLength - 1; xsLoc = "R"; }
                                        }
                                        if (!int.TryParse(ysLoc, out int yLoc))
                                        {
                                            if (ysLoc == "top" || ysLoc == "t") { yLoc = 0; ysLoc = "T"; }
                                            else if (ysLoc == "mid" || ysLoc == "m") { yLoc = (yLength - 1) / 2; ysLoc = "M"; }
                                            else if (ysLoc == "bottom" || ysLoc == "bot" || ysLoc == "b") { yLoc = yLength - 1; ysLoc = "B"; }
                                        }

                                        if (xLoc < 0)
                                        {
                                            xLoc = xLength - (1 + xLoc / -1);
                                            xsLoc = xLoc.ToString();
                                        }
                                        if (yLoc < 0)
                                        {
                                            yLoc = yLength - (1 + yLoc / -1);
                                            ysLoc = yLoc.ToString();
                                        }

                                        var row = AMGState.map.data[yLoc].ToCharArray();
                                        row[xLoc] = oTile.tileCode;
                                        AMGState.map.data[yLoc] = string.Join("", row);

                                        Logger.LogTile(new TileActionTypes(1, 0, 1, 0, 0), oTile, ysLoc, xsLoc, yLength, xLength, Logger.TileEvent.gamemodeModding);
                                    }

                    }

                    Options1.SpecialTileRules[] str = null;
                    if (batchOption.specialTileRules != null)
                        str = batchOption.specialTileRules;

                    for (int drawPass = 0; drawPass < 2; drawPass++)
                    {
                        int currentY = 0;
                        int currentX = 0;

                        List<OrderedTile> orderedTiles = new List<OrderedTile>();
                        List<Options1.RecordedSTR> rstr = new List<Options1.RecordedSTR>();

                        Logger.Title.Status.UpdateStatus(0, xLength * yLength, string.Format("Drawing tiles... PASS {0}", drawPass));
                        // Begin to draw map
                        foreach (string row in AMGState.map.data)
                        {
                            List<OrderedTile> orderedHorTiles = new List<OrderedTile>();

                            foreach (char tTile in row.ToCharArray())
                            {
                                Logger.Title.Status.IncreaseStatus();
                                Logger.Title.RefreshTitle();

                                if (batchOption.mapMetadata != null)
                                {
                                    foreach (string metaKey in batchOption.mapMetadata.Keys)
                                        foreach (var datum in batchOption.mapMetadata[metaKey])
                                            if (datum.x == currentX && datum.y == currentY)
                                            {
                                                if (!tiledata.metadata.TryGetValue(metaKey, out var mTiles))
                                                    continue;

                                                var mTile = mTiles[datum.t];

                                                foreach (var lTile in tiledata.tiles)
                                                {
                                                    if (lTile.tileName == mTile.tile)
                                                    {
                                                        var found = lTile.tileTypes[mTile.type];

                                                        if ((drawPass == 0 && found.order.GetValueOrDefault() < 0) ||
                                                            (drawPass == 1 && found.order.GetValueOrDefault() >= 0))
                                                        {
                                                            Logger.LogTile(new TileActionTypes(1, 0, 0, 1, 0, 0), lTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                                            orderedTiles.Add(new OrderedTile()
                                                            {
                                                                tileTypeData = found,
                                                                tileType = mTile.type,
                                                                xPosition = currentX,
                                                                yPosition = currentY,
                                                                tileCode = lTile.tileCode,
                                                                tileName = lTile.tileName
                                                            });
                                                        }
                                                    }
                                                }
                                            }
                                }

                                bool tileDrawn = false;

                                var tile = tTile;

                                if (batchOption.replaceTiles != null)
                                    foreach (Options1.Replace repTile in batchOption.replaceTiles) // Specified Tile Code Replacer
                                        if (tile == repTile.from)
                                            tile = repTile.to;

                                if (batchOption.skipTiles.Contains(tile)) // Specified Tile Skipper
                                {
                                    Logger.LogTile(new TileActionTypes(0, 1, 0, 0, 0), new Tiledata.Tile() { tileName = "", tileCode = tile }, currentY, currentX, yLength, xLength, Logger.TileEvent.tileDraw);
                                    currentX++;
                                    tileDrawn = true;
                                    continue;
                                }

                                if (tiledata.ignoreTiles.Contains(tile)) // Specified Tile Ignorer
                                {
                                    currentX++;
                                    tileDrawn = true;
                                    continue;
                                }

                                // Checking STR (Special Tile Rules) Tiles' occurance number and acting if conditions are met
                                if (str != null)
                                {
                                    bool drawn = false;
                                    foreach (var ostr in str)
                                        if (ostr.tileCode == tile)
                                        {
                                            Options1.RecordRSTR(rstr, tile);
                                            foreach (var orstr in rstr)
                                                if (orstr.tileCode == tile)
                                                    if (ostr.tileTime == orstr.tileTime)
                                                        foreach (var aTile in tiledata.tiles)
                                                            if (aTile.tileCode == tile)
                                                            {
                                                                // Save tile for later drawing (Ordering and Horizontal Ordering)
                                                                if (aTile.tileTypes[ostr.tileType].order != null)
                                                                {
                                                                    if ((drawPass == 0 && aTile.tileTypes[ostr.tileType].order < 0) ||
                                                                        (drawPass == 1 && aTile.tileTypes[ostr.tileType].order >= 0))
                                                                    {
                                                                        Logger.LogTile(new TileActionTypes(0, 1, 1, 0, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                                                        orderedTiles.Add(new OrderedTile()
                                                                        {
                                                                            tileTypeData = aTile.tileTypes[ostr.tileType],
                                                                            tileType = ostr.tileType,
                                                                            xPosition = currentX,
                                                                            yPosition = currentY,
                                                                            tileCode = aTile.tileCode,
                                                                            tileName = aTile.tileName,
                                                                            str = true
                                                                        });
                                                                    }
                                                                    drawn = true;
                                                                    break;
                                                                }
                                                                if (aTile.tileTypes[ostr.tileType].orderHor != null)
                                                                {
                                                                    if ((drawPass == 0 && aTile.tileTypes[ostr.tileType].orderHor < 0) ||
                                                                        (drawPass == 1 && aTile.tileTypes[ostr.tileType].orderHor >= 0))
                                                                    {
                                                                        Logger.LogTile(new TileActionTypes(0, 1, 1, 1, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedHorTileDraw);
                                                                        orderedHorTiles.Add(new OrderedTile()
                                                                        {
                                                                            tileTypeData = aTile.tileTypes[ostr.tileType],
                                                                            tileType = ostr.tileType,
                                                                            xPosition = currentX,
                                                                            yPosition = currentY,
                                                                            tileCode = aTile.tileCode,
                                                                            tileName = aTile.tileName,
                                                                            str = true
                                                                        });
                                                                    }
                                                                    drawn = true;
                                                                    break;
                                                                }

                                                                // Draw STR Tile
                                                                if (drawPass == 1)
                                                                {
                                                                    tileDrawer.DrawTile(aTile, ostr.tileType, options, sizeMultiplier, currentX, currentY, selectedTileImageList, border);
                                                                    AMGState.drawer.DrawnTile();
                                                                    Logger.LogTile(new TileActionTypes(0, 1, 0, 0, 1), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.tileDraw);
                                                                }

                                                                drawn = true;

                                                                break;
                                                            }
                                        }
                                    if (drawn)
                                    {
                                        currentX++;
                                        tileDrawn = true;
                                        continue;
                                    }
                                }

                                string NeighborBinary = "";

                                foreach (Tiledata.Tile aTile in tiledata.tiles)
                                {
                                    if (aTile.tileCode == tile) // Loop until tile found matching with data
                                    {
                                        Tiledata.TileDefault setTileDefault = null;
                                        foreach (Tiledata.TileDefault tileDefault in mapBiome.defaults)
                                        {
                                            setTileDefault = tileDefault;
                                            if (batchOption.overrideBiome != null) // Biome overrider
                                                foreach (var overrideTile in batchOption.overrideBiome)
                                                    if (overrideTile.tile == tileDefault.tile)
                                                    {
                                                        setTileDefault = overrideTile;
                                                        break;
                                                    }

                                            if (batchOption.gamemode != null && mapGamemode != null && mapGamemode.overrideBiome != null) // Biome overrider (from Gamemode options)
                                                foreach (var overrideTile in mapGamemode.overrideBiome)
                                                    if (overrideTile.tile == tileDefault.tile)
                                                    {
                                                        setTileDefault = overrideTile;
                                                        break;
                                                    }

                                            if (setTileDefault.tile == aTile.tileName)
                                            {
                                                if (aTile.tileLinks != null) // Check if Tile Links are set
                                                {
                                                    NeighborBinary = TileDrawer.tileLinks(AMGState.map.data, currentX, currentY, aTile, batchOption.replaceTiles);

                                                    List<Tiledata.TileLinkRule> accurateRules = new List<Tiledata.TileLinkRule>();

                                                    var nbca = NeighborBinary.ToCharArray(); // Get neighboring tiles in binary
                                                    if (aTile.tileLinks.rules.Length != 0)
                                                    {
                                                        // Check if tile rule is matching and act
                                                        foreach (var rule in aTile.tileLinks.rules)
                                                        {
                                                            int accuracy = 0;
                                                            for (int x = 0; x < 8; x++)
                                                            {
                                                                if (rule.condition.Contains('!'))
                                                                {
                                                                    if (rule.condition.Replace("!", "").ToCharArray()[x] == '*')
                                                                        accuracy++;
                                                                    else if (rule.condition.Replace("!", "").ToCharArray()[x] != nbca[x])
                                                                        accuracy++;
                                                                }
                                                                else
                                                                {
                                                                    if (rule.condition.ToCharArray()[x] == '*')
                                                                        accuracy++;
                                                                    else if (rule.condition.ToCharArray()[x] == nbca[x])
                                                                        accuracy++;
                                                                }
                                                            }

                                                            if (accuracy == 8)
                                                            {
                                                                if (rule.requiredBiome != null)
                                                                {

                                                                    if (rule.requiredBiome.GetValueOrDefault() == batchOption.biome)
                                                                    {
                                                                        accurateRules.Add(rule);
                                                                        if (!aTile.tileLinks.multipleConditionsCouldApply)
                                                                            break;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    accurateRules.Add(rule);
                                                                    if (!aTile.tileLinks.multipleConditionsCouldApply)
                                                                        break;
                                                                }
                                                            }
                                                        }
                                                    }

                                                    string fols = "";

                                                    // Do actions specified in the rule which were correct
                                                    var defaultType = aTile.tileTypes[aTile.tileLinks.defaults.tileType];
                                                    int type = aTile.tileLinks.defaults.tileType;
                                                    foreach (Tiledata.TileLinkRule aRule in accurateRules)
                                                    {
                                                        if (aRule.changeBinary != null)
                                                            for (int y = 0; y < aRule.changeBinary.Length; y++)
                                                            {
                                                                nbca[int.Parse(aRule.changeBinary[y].Split('a')[1])] = aRule.changeBinary[y].Split('a')[0].ToCharArray()[0];
                                                            }
                                                        if (aRule.changeTileType != null)
                                                        {
                                                            defaultType = aTile.tileTypes[aRule.changeTileType.GetValueOrDefault()];
                                                            type = aRule.changeTileType.GetValueOrDefault();
                                                        }
                                                        if (aRule.changeFolder != null && aTile.tileLinks.assetFolder != null)
                                                            fols = aRule.changeFolder + "/";
                                                    }

                                                    var defaultAsset = defaultType.asset;

                                                    var fullBinaryFinal = string.Join("", nbca);

                                                    if (defaultAsset.Contains("?binary?"))
                                                        defaultAsset = defaultAsset.Replace("?binary?", fullBinaryFinal);

                                                    if (aTile.tileLinks.assetFolder != null && fols == "")
                                                        fols = aTile.tileLinks.assetFolder + "/";
                                                    var assetst = fullBinaryFinal + ".svg";

                                                    // Make a copy of the tile (not reference)
                                                    Tiledata.TileType breakerTile = new Tiledata.TileType()
                                                    {
                                                        asset = fols + defaultAsset,
                                                        color = defaultType.color,
                                                        detailed = defaultType.detailed,
                                                        order = defaultType.order,
                                                        orderHor = defaultType.orderHor,
                                                        other = defaultType.other,
                                                        tileParts = defaultType.tileParts,
                                                        visible = defaultType.visible,
                                                    };

                                                    // Save tile for later drawing (Ordering and Horizontal Ordering)
                                                    if (defaultType.order != null)
                                                    {
                                                        if ((drawPass == 0 && defaultType.order < 0) ||
                                                            (drawPass == 1 && defaultType.order >= 0))
                                                        {
                                                            Logger.LogTile(new TileActionTypes(0, 0, 1, 0, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                                            orderedTiles.Add(new OrderedTile()
                                                            {
                                                                tileTypeData = breakerTile,
                                                                tileType = type,
                                                                xPosition = currentX,
                                                                yPosition = currentY,
                                                                tileCode = aTile.tileCode,
                                                                tileName = aTile.tileName
                                                            });
                                                        }
                                                        tileDrawn = true;
                                                        break;
                                                    }
                                                    if (defaultType.orderHor != null)
                                                    {
                                                        if ((drawPass == 0 && defaultType.orderHor < 0) ||
                                                            (drawPass == 1 && defaultType.orderHor >= 0))
                                                        {
                                                            Logger.LogTile(new TileActionTypes(0, 0, 1, 1, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedHorTileDraw);
                                                            orderedHorTiles.Add(new OrderedTile()
                                                            {
                                                                tileTypeData = breakerTile,
                                                                tileType = type,
                                                                xPosition = currentX,
                                                                yPosition = currentY,
                                                                tileCode = aTile.tileCode,
                                                                tileName = aTile.tileName
                                                            });
                                                        }
                                                        tileDrawn = true;
                                                        break;
                                                    }

                                                    // Draw Tile
                                                    if (drawPass == 1)
                                                    {
                                                        tileDrawer.DrawSelectedTile(
                                                            new OrderedTile() {
                                                                tileTypeData = breakerTile, 
                                                                tileType = type,
                                                                xPosition = currentX,
                                                                yPosition = currentY,
                                                                tileCode = aTile.tileCode,
                                                                tileName = aTile.tileName
                                                            }, options, sizeMultiplier, selectedTileImageList, border);
                                                        Logger.LogTile(new TileActionTypes(0, 0, 0, 0, 1), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.tileDraw);
                                                        AMGState.drawer.DrawnTile();
                                                    }
                                                    tileDrawn = true;
                                                    break;
                                                }

                                                // Save tile for later drawing (Ordering and Horizontal Ordering)
                                                if (aTile.tileTypes[setTileDefault.type].order != null)
                                                {
                                                    if ((drawPass == 0 && aTile.tileTypes[setTileDefault.type].order < 0) ||
                                                        (drawPass == 1 && aTile.tileTypes[setTileDefault.type].order >= 0))
                                                    {
                                                        Logger.LogTile(new TileActionTypes(0, 0, 1, 0, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                                        orderedTiles.Add(new OrderedTile()
                                                        {
                                                            tileTypeData = aTile.tileTypes[setTileDefault.type],
                                                            tileType = setTileDefault.type,
                                                            xPosition = currentX,
                                                            yPosition = currentY,
                                                            tileCode = aTile.tileCode,
                                                            tileName = aTile.tileName
                                                        });
                                                    }
                                                    tileDrawn = true;
                                                    break;
                                                }
                                                if (aTile.tileTypes[setTileDefault.type].orderHor != null)
                                                {
                                                    if ((drawPass == 0 && aTile.tileTypes[setTileDefault.type].orderHor < 0) ||
                                                        (drawPass == 1 && aTile.tileTypes[setTileDefault.type].orderHor >= 0))
                                                    {
                                                        Logger.LogTile(new TileActionTypes(0, 0, 1, 1, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedHorTileDraw);
                                                        orderedHorTiles.Add(new OrderedTile()
                                                        {
                                                            tileTypeData = aTile.tileTypes[setTileDefault.type],
                                                            tileType = setTileDefault.type,
                                                            xPosition = currentX,
                                                            yPosition = currentY,
                                                            tileCode = aTile.tileCode,
                                                            tileName = aTile.tileName
                                                        });
                                                    }
                                                    tileDrawn = true;
                                                    break;
                                                }

                                                // Draw Tile
                                                if (drawPass == 1)
                                                {
                                                    tileDrawer.DrawTile(aTile, setTileDefault.type, options, sizeMultiplier, currentX, currentY, selectedTileImageList, border);
                                                    AMGState.drawer.DrawnTile();
                                                    Logger.LogTile(new TileActionTypes(0, 0, 0, 0, 1), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.tileDraw);
                                                }
                                                tileDrawn = true;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!tileDrawn)
                                {
                                    if (!tilesFailed.ContainsKey(tTile))
                                    {
                                        tilesFailed.Add(tTile, 1);
                                    }
                                    else
                                        tilesFailed[tTile]++;
                                }

                                currentX++;

                            }

                            // Draw Horizontally Ordered Tiles
                            int lowestHorOrder = 0, highestHorOrder = 0;
                            foreach (var pTile in orderedHorTiles)
                            {
                                if (pTile == null)
                                    continue;

                                var value = pTile.tileTypeData.orderHor.GetValueOrDefault();

                                if (value > highestHorOrder)
                                    highestHorOrder = value;
                                if (value < lowestHorOrder)
                                    lowestHorOrder = value;
                            }

                            for (int currentHorOrdered = lowestHorOrder; currentHorOrdered <= highestHorOrder; currentHorOrdered++)
                                foreach (var pTile in orderedHorTiles)
                                {
                                    if (pTile == null)
                                        continue;
                                    if (pTile.tileTypeData.orderHor.GetValueOrDefault() != currentHorOrdered)
                                        continue;

                                    tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, selectedTileImageList, border);
                                    if (pTile.str)
                                        Logger.LogTile(new TileActionTypes(0, 1, 1, 1, 1), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength, Logger.TileEvent.orderedHorTileDraw);
                                    else
                                        Logger.LogTile(new TileActionTypes(0, 0, 1, 1, 1), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength, Logger.TileEvent.orderedHorTileDraw);
                                    AMGState.drawer.DrawnTile();
                                }

                            currentX = 0;
                            currentY++;

                        }

                        // Draw Ordered Tiles
                        int lowestOrder = 0, highestOrder = 0;
                        foreach (var pTile in orderedTiles)
                        {
                            if (pTile == null)
                                continue;

                            var value = pTile.tileTypeData.order.GetValueOrDefault();

                            if (value > highestOrder)
                                highestOrder = value;
                            if (value < lowestOrder)
                                lowestOrder = value;
                        }

                        for (int currentOrdered = lowestOrder; currentOrdered <= highestOrder; currentOrdered++)
                            foreach (var pTile in orderedTiles)
                            {
                                if (pTile == null)
                                    continue;
                                if (pTile.tileTypeData.order.GetValueOrDefault() != currentOrdered)
                                    continue;

                                tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, selectedTileImageList, border);
                                if (pTile.str)
                                    Logger.LogTile(new TileActionTypes(0, 1, 1, 0, 1), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                else
                                    Logger.LogTile(new TileActionTypes(0, 0, 1, 0, 1), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                AMGState.drawer.DrawnTile();
                            }

                        if (mapGamemode != null && drawPass == 1) // Draw Gamemode tiles (after everything else)
                            if (mapGamemode.specialTiles != null)
                                foreach (var st in mapGamemode.specialTiles)
                                {
                                    if (st.drawOrder == 2)
                                    {
                                        foreach (Tiledata.Tile oTile in tiledata.tiles)
                                        {
                                            if (oTile.tileName == st.tile)
                                            {
                                                string xsLoc = st.position.Split(',')[0].Trim().ToLower();
                                                string ysLoc = st.position.Split(',')[1].Trim().ToLower();
                                                if (!int.TryParse(xsLoc, out int xLoc))
                                                {
                                                    if (xsLoc == "left" || xsLoc == "l") { xLoc = 0; xsLoc = "L"; }
                                                    else if (xsLoc == "mid" || xsLoc == "m") { xLoc = (xLength - 1) / 2; xsLoc = "M"; }
                                                    else if (xsLoc == "right" || xsLoc == "r") { xLoc = xLength - 1; xsLoc = "R"; }
                                                }
                                                if (!int.TryParse(ysLoc, out int yLoc))
                                                {
                                                    if (ysLoc == "top" || ysLoc == "t") { yLoc = 0; ysLoc = "T"; }
                                                    else if (ysLoc == "mid" || ysLoc == "m") { yLoc = (yLength - 1) / 2; ysLoc = "M"; }
                                                    else if (ysLoc == "bottom" || ysLoc == "bot" || ysLoc == "b") { yLoc = yLength - 1; ysLoc = "B"; }
                                                }

                                                if (xLoc < 0)
                                                {
                                                    xLoc = xLength - (1 + xLoc / -1);
                                                    xsLoc = xLoc.ToString();
                                                }
                                                if (yLoc < 0)
                                                {
                                                    yLoc = yLength - (1 + yLoc / -1);
                                                    ysLoc = yLoc.ToString();
                                                }

                                                tileDrawer.DrawTile(oTile, st.type, options, sizeMultiplier, xLoc, yLoc, selectedTileImageList, border);
                                                AMGState.drawer.DrawnTile();
                                                Logger.LogTile(new TileActionTypes(1, 0, 0, 0, 1), oTile, ysLoc, xsLoc, yLength, xLength, Logger.TileEvent.tileDraw);
                                            }
                                        }
                                    }
                                }

                    }

                    string exportName = options.exportFileName;

                    if (exportName.Contains("?number?"))
                    {
                        string bNumberText = Logger.LeftSpaceFiller(AMGState.map.index, 4, '0');

                        exportName = exportName.Replace("?number?", bNumberText);
                    }

                    // Save map image
                    Logger.LogStatus("Map drawn.");
                    if (options.exportFolderName != null)
                    {
                        if (options.exportFolderName.Trim() != "")
                        {
                            if (batchOption.exportFileName != null)
                            {
                                exportName = batchOption.exportFileName;
                                if (batchOption.exportFileName.Contains("?number?"))
                                {
                                    string bNumberText = Logger.LeftSpaceFiller(AMGState.map.index, 4, '0');
                                    exportName = exportName.Replace("?number?", bNumberText);
                                }

                                tileDrawer.ExportImage(options, exportName);
                                Logger.LogAAL(Logger.AALDirection.Out, options.exportFolderName + "/" + exportName);
                                Logger.LogExport(exportName);
                            }
                            else
                            {
                                tileDrawer.ExportImage(options, exportName);
                                Logger.LogAAL(Logger.AALDirection.Out, options.exportFolderName + "/" + exportName);
                                Logger.LogExport(exportName);
                            }
                        }
                        else
                        {
                            if (batchOption.exportFileName != null)
                                tileDrawer.ExportImage(options, batchOption.exportFileName);
                            else
                                tileDrawer.ExportImage(options, exportName);
                            Logger.LogAAL(Logger.AALDirection.Out, exportName);
                            Logger.LogExport(exportName);
                        }
                    }
                    else
                    {
                        if (batchOption.exportFileName != null)
                            tileDrawer.ExportImage(options, batchOption.exportFileName);
                        else
                            tileDrawer.ExportImage(options, exportName);
                        Logger.LogAAL(Logger.AALDirection.Out, exportName);
                        Logger.LogExport(exportName);
                    }

                    AMGState.drawer.DrawnTile();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

            AMGState.StopTimer();
            Logger.LogSpacer();
            Logger.Log("Finished.");

            string stTime = "";
            if (AMGState.time / 86400000 != 0)
                stTime = (AMGState.time / 86400000) + "d " + (AMGState.time / 3600000 - AMGState.time / 86400000 * 24) + "h " + (AMGState.time / 60000 - AMGState.time / 3600000 * 60) + "m " + (AMGState.time / 1000 - AMGState.time / 60000 * 60) + "s " + (AMGState.time - AMGState.time / 1000 * 1000) + "ms";
            else if (AMGState.time / 3600000 != 0)
                stTime = (AMGState.time / 3600000) + "h " + (AMGState.time / 60000 - AMGState.time / 3600000 * 60) + "m " + (AMGState.time / 1000 - AMGState.time / 60000 * 60) + "s " + (AMGState.time - AMGState.time / 1000 * 1000) + "ms";
            else if (AMGState.time / 60000 != 0)
                stTime = (AMGState.time / 60000) + "m " + (AMGState.time / 1000 - AMGState.time / 60000 * 60) + "s " + (AMGState.time - AMGState.time / 1000 * 1000) + "ms";
            else if (AMGState.time / 1000 != 0)
                stTime = (AMGState.time / 1000) + "s " + (AMGState.time - AMGState.time / 1000 * 1000) + "ms";
            else
                stTime = AMGState.time + "ms";

            Logger.LogSpacer();
            Logger.Log("Results:\n  Total Maps Drawn: " + AMGState.drawer.mapsDrawn + "\n  Total Tiles Drawn: " + AMGState.drawer.tilesDrawn + "\n  Completed in: " + stTime);

            if (tilesFailed.Count == 0)
                Logger.Log("  No unrecognized tiles encountered.");
            else
            {
                Logger.Log("  Unrecognized tiles encountered:");
                foreach (var t in tilesFailed.Keys)
                    Logger.Log("    \"" + t + "\": " + tilesFailed[t]);
            }

            Logger.LogSpacer();

            Logger.Save("log.txt");

            Console.ReadKey();

            if (oEnd.ToLower() == "pause")
                Console.ReadKey();

        }

    }

    

    
    */
}
