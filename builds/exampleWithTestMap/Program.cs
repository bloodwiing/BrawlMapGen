using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using Svg;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace generator
{
    class Program
    {
        static void Main(string[] args)
        {
            var fArgs = " " + string.Join(" ", args);
            string oLoc = "options.json";
            string oStr = "";
            string oEnd = "";
            Options options = new Options();
            Voice voice = new Voice();

            try
            {
                if (fArgs.ToLower().Contains(" -f "))
                {
                    oLoc = fArgs.Replace(" -f ", "@").Replace(" -F ", "@").Split('@')[1];
                    if (oLoc.Contains(" -"))
                        oLoc = oLoc.Replace(" -", "#").Split('#')[0].Trim();
                    else
                        oLoc = oLoc.Trim();
                }

                if (fArgs.ToLower().Contains(" -os "))
                {
                    oStr = fArgs.Replace(" -os ", "@").Replace(" -Os ", "@").Replace(" -OS ", "@").Replace(" -oS ", "@").Split('@')[1];
                    if (oStr.Contains(" -"))
                        oStr = oStr.Replace(" -", "#").Split('#')[0].Trim();
                    else
                        oStr = oStr.Trim();
                }

                if (fArgs.ToLower().Contains(" -e "))
                {
                    oEnd = fArgs.Replace(" -e ", "@").Replace(" -E ", "@").Split('@')[1].Trim();
                    if (oEnd.Contains(" -"))
                        oEnd = oEnd.Replace(" -", "#").Split('#')[0].Trim();
                    else
                        oEnd = oEnd.Trim();
                }

                if (oStr == "")
                {
                    if (!File.Exists(oLoc))
                    {
                        voice.Speak("\nERROR:\nOption file doesn't exist\n[FileReader] Unable to find file in location \"" + oLoc + "\"", ActionType.basic);
                        voice.Write("log.txt");
                        Thread.Sleep(3000);
                        Environment.Exit(1);
                    }

                    StreamReader r = new StreamReader(oLoc);
                    string json = r.ReadToEnd();
                    options = JsonConvert.DeserializeObject<Options>(json);
                }
                else
                    options = JsonConvert.DeserializeObject<Options>(oStr);

                voice.UpdateOptions(options);

                if (options.setPath != null)
                    Environment.CurrentDirectory = options.setPath;

                voice.Speak("Brawl Map Gen v1.5\nCreated by RedH1ghway aka TheDonciuxx\nWith the help of 4JR\n\nLoading " + options.preset + " preset", ActionType.setup);
                voice.Speak("[ AAL ] READ << presets\\" + options.preset + ".json", ActionType.aal);

                if (!File.Exists("presets\\" + options.preset + ".json"))
                {
                    voice.Speak("\nERROR:\nPreset doesn't exist\n[FileReader] Unable to find file in location \"presets\\" + options.preset + ".json\"", ActionType.basic);
                    voice.Write("log.txt");
                    Thread.Sleep(3000);
                    Environment.Exit(1);
                }

                StreamReader r2 = new StreamReader("presets\\" + options.preset + ".json");
                string json2 = r2.ReadToEnd();
                var tiledata = JsonConvert.DeserializeObject<Tiledata>(json2);

                voice.Speak("Preset \"" + options.preset.ToUpper() + "\" loaded.", ActionType.setup);

                int bNumber = 0;
                foreach (var batchOption in options.batch)
                {
                    bNumber++;

                    voice.Speak("\nReading the map in the index number " + bNumber + "...", ActionType.setup);

                    var map = batchOption.map;
                    var sizeMultiplier = batchOption.sizeMultiplier;

                    if (map == null)
                    {
                        voice.Speak("\nWARNING:\nMap is empty!\n[Object] Map in the index number " + bNumber + " is not defined.", ActionType.basic);
                        Thread.Sleep(3000);
                        continue;
                    }
                    else if (map.Length == 0)
                    {
                        voice.Speak("\nWARNING:\nMap is empty!\n[Object] Map in the index number " + bNumber + " has no string arrays.", ActionType.basic);
                        Thread.Sleep(3000);
                        continue;
                    }

                    int xLength = map[0].Length;
                    int yLength = map.Length;

                    voice.Speak("Updating info...\n\nImage size set to " + (sizeMultiplier * 2 + sizeMultiplier * xLength) + "px width and " + (sizeMultiplier * 2 + sizeMultiplier * yLength) + "px height.\nBiome set to \"" + tiledata.biomes[batchOption.biome - 1].name.ToUpper() + "\"\n", ActionType.setup);

                    TileDrawer tileDrawer = new TileDrawer(batchOption.sizeMultiplier, batchOption.map[0].Length, batchOption.map.Length);

                    int currentY = 0;
                    int currentX = 0;

                    voice.Speak("Fetching tile colors...", ActionType.setup);

                    string[] color1s = tiledata.biomes[batchOption.biome - 1].color1.Split(',');
                    Color color1 = Color.FromArgb(int.Parse(color1s[0].Trim()), int.Parse(color1s[1].Trim()), int.Parse(color1s[2].Trim()));

                    string[] color2s = tiledata.biomes[batchOption.biome - 1].color2.Split(',');
                    Color color2 = Color.FromArgb(int.Parse(color2s[0].Trim()), int.Parse(color2s[1].Trim()), int.Parse(color2s[2].Trim()));

                    voice.Speak("Coloring the tiles...", ActionType.setup);

                    tileDrawer.ColorBackground(color1, color2, map, batchOption);

                    List<OrderedTile> orderedTiles = new List<OrderedTile>();

                    if (batchOption.name != null)
                    {
                        voice.Speak("Drawing map \"" + batchOption.name.ToUpper() + "\"...", ActionType.basic);
                    }
                    else
                    {
                        voice.Speak("Drawing map no " + bNumber + "...", ActionType.basic);
                    }

                    Tiledata.Gamemode mapGamemode = null;
                    foreach (var gm in tiledata.gamemodes)
                    {
                        if (gm == null || batchOption.gamemode == null)
                            break;
                        if (gm.name == batchOption.gamemode)
                            mapGamemode = gm;
                    }

                    if (mapGamemode != null)
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

                                        tileDrawer.DrawTile(oTile, st.type, options, sizeMultiplier, xLoc, yLoc, xLength, yLength);
                                        voice.Speak(TileActionStringMaker(new TileActionTypes(true, false, false, false, true), oTile, ysLoc, xsLoc, yLength, xLength), ActionType.tileDraw);
                                    }
                                }
                            }
                        }

                    Options.SpecialTileRules[] str = null;
                    if (batchOption.specialTileRules != null)
                        str = batchOption.specialTileRules;

                    List<Options.RecordedSTR> rstr = new List<Options.RecordedSTR>();

                    foreach (string row in map)
                    {
                        List<OrderedTile> orderedHorTiles = new List<OrderedTile>();

                        foreach (char tTile in row.ToCharArray())
                        {
                            if (batchOption.skipTiles.Contains(tTile))
                            {
                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, false, false, false), new Tiledata.Tile() { tileName = "", tileCode = tTile }, currentY, currentX, yLength, xLength), ActionType.tileDraw);
                                currentX++;
                                continue;
                            }

                            var tile = tTile;

                            foreach (Options.Replace repTile in batchOption.replaceTiles)
                            {
                                if (tile == repTile.from)
                                    tile = repTile.to;
                            }

                            if (str != null)
                            {
                                bool drawn = false;
                                foreach (var ostr in str)
                                    if (ostr.tileCode == tile)
                                    {
                                        Options.RecordRSTR(rstr, tile);
                                        foreach (var orstr in rstr)
                                            if (orstr.tileCode == tile)
                                                if (ostr.tileTime == orstr.tileTime)
                                                    foreach (var aTile in tiledata.tiles)
                                                        if (aTile.tileCode == tile)
                                                        {
                                                            if (aTile.tileTypes[ostr.tileType - 1].order != null)
                                                            {
                                                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, true, false, false), aTile, currentY, currentX, yLength, xLength), ActionType.orderedTileDraw);
                                                                orderedTiles.Add(new OrderedTile()
                                                                {
                                                                    tileType = aTile.tileTypes[ostr.tileType - 1],
                                                                    xPosition = currentX,
                                                                    yPosition = currentY,
                                                                    tileCode = aTile.tileCode,
                                                                    tileName = aTile.tileName,
                                                                    str = true
                                                                });
                                                                drawn = true;
                                                                break;
                                                            }
                                                            if (aTile.tileTypes[ostr.tileType - 1].orderHor != null)
                                                            {
                                                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, true, true, false), aTile, currentY, currentX, yLength, xLength), ActionType.orderedHorTileDraw);
                                                                orderedHorTiles.Add(new OrderedTile()
                                                                {
                                                                    tileType = aTile.tileTypes[ostr.tileType - 1],
                                                                    xPosition = currentX,
                                                                    yPosition = currentY,
                                                                    tileCode = aTile.tileCode,
                                                                    tileName = aTile.tileName,
                                                                    str = true
                                                                });
                                                                drawn = true;
                                                                break;
                                                            }

                                                            tileDrawer.DrawTile(aTile, ostr.tileType, options, sizeMultiplier, currentX, currentY, xLength, yLength);
                                                            voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, false, false, true), aTile, currentY, currentX, yLength, xLength), ActionType.tileDraw);
                                                            
                                                            drawn = true;

                                                            break;
                                                        }
                                    }
                                if (drawn)
                                {
                                    currentX++;
                                    continue;
                                }
                            }

                            string NeighborBinary = "";

                            foreach (Tiledata.Tile aTile in tiledata.tiles)
                            {
                                if (aTile.tileCode == tile)
                                {
                                    Tiledata.TileDefault setTileDefault = null;
                                    foreach (Tiledata.TileDefault tileDefault in tiledata.biomes[batchOption.biome - 1].defaults)
                                    {
                                        setTileDefault = tileDefault;
                                        if (batchOption.overrideBiome != null)
                                            foreach (var overrideTile in batchOption.overrideBiome)
                                                if (overrideTile.tile == tileDefault.tile)
                                                {
                                                    setTileDefault = overrideTile;
                                                    break;
                                                }

                                        if (setTileDefault.tile == aTile.tileName)
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
                                                            accurateRules.Add(rule);
                                                    }
                                                }

                                                var defaultType = aTile.tileTypes[aTile.tileLinks.defaults.tileType - 1];
                                                foreach (Tiledata.TileLinkRule aRule in accurateRules)
                                                {
                                                    if (aRule.changeBinary != null)
                                                        for (int y = 0; y < aRule.changeBinary.Length; y++)
                                                        {
                                                            nbca[int.Parse(aRule.changeBinary[y].Split('a')[1]) - 1] = aRule.changeBinary[y].Split('a')[0].ToCharArray()[0];
                                                        }
                                                    if (aRule.changeTileType != null)
                                                        defaultType = aTile.tileTypes[aRule.changeTileType.GetValueOrDefault() - 1];
                                                }

                                                var defaultAsset = defaultType.asset;

                                                var fullBinaryFinal = string.Join("", nbca);

                                                if (defaultAsset.Contains("?binary?"))
                                                    defaultAsset = defaultAsset.Replace("?binary?", fullBinaryFinal);

                                                string fols = "";
                                                if (aTile.tileLinks.assetFolder != null)
                                                    fols = aTile.tileLinks.assetFolder + "\\";
                                                var assetst = fullBinaryFinal + ".svg";

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

                                                if (defaultType.order != null)
                                                {
                                                    voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, false, false), aTile, currentY, currentX, yLength, xLength), ActionType.orderedTileDraw);
                                                    orderedTiles.Add(new OrderedTile()
                                                    {
                                                        tileType = breakerTile,
                                                        xPosition = currentX,
                                                        yPosition = currentY,
                                                        tileCode = aTile.tileCode,
                                                        tileName = aTile.tileName
                                                    });
                                                    break;
                                                }
                                                if (defaultType.orderHor != null)
                                                {
                                                    voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, true, false), aTile, currentY, currentX, yLength, xLength), ActionType.orderedHorTileDraw);
                                                    orderedHorTiles.Add(new OrderedTile()
                                                    {
                                                        tileType = breakerTile,
                                                        xPosition = currentX,
                                                        yPosition = currentY,
                                                        tileCode = aTile.tileCode,
                                                        tileName = aTile.tileName
                                                    });
                                                    break;
                                                }
                                                
                                                tileDrawer.DrawSelectedTile(new OrderedTile() { tileType = breakerTile, xPosition = currentX, yPosition = currentY, tileCode = aTile.tileCode, tileName = aTile.tileName }, options, sizeMultiplier, xLength, yLength);
                                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, false, false, true), aTile, currentY, currentX, yLength, xLength), ActionType.tileDraw);
                                                break;
                                            }

                                            if (aTile.tileTypes[setTileDefault.type - 1].order != null)
                                            {
                                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, false, false), aTile, currentY, currentX, yLength, xLength), ActionType.orderedTileDraw);
                                                orderedTiles.Add(new OrderedTile()
                                                {
                                                    tileType = aTile.tileTypes[setTileDefault.type - 1],
                                                    xPosition = currentX,
                                                    yPosition = currentY,
                                                    tileCode = aTile.tileCode,
                                                    tileName = aTile.tileName
                                                });
                                                break;
                                            }
                                            if (aTile.tileTypes[setTileDefault.type - 1].orderHor != null)
                                            {
                                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, true, false), aTile, currentY, currentX, yLength, xLength), ActionType.orderedHorTileDraw);
                                                orderedHorTiles.Add(new OrderedTile()
                                                {
                                                    tileType = aTile.tileTypes[setTileDefault.type - 1],
                                                    xPosition = currentX,
                                                    yPosition = currentY,
                                                    tileCode = aTile.tileCode,
                                                    tileName = aTile.tileName
                                                });
                                                break;
                                            }

                                            tileDrawer.DrawTile(aTile, setTileDefault.type, options, sizeMultiplier, currentX, currentY, xLength, yLength);
                                            voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, false, false, true), aTile, currentY, currentX, yLength, xLength), ActionType.tileDraw);
                                            break;
                                        }
                                    }
                                }
                            }

                            currentX++;

                        }

                        int highestHorOrder = 1;
                        foreach (var pTile in orderedHorTiles)
                        {
                            if (pTile == null)
                                continue;
                            if (pTile.tileType.orderHor.GetValueOrDefault() > highestHorOrder)
                                highestHorOrder = pTile.tileType.orderHor.GetValueOrDefault();
                            if (pTile.tileType.orderHor.GetValueOrDefault() != 1)
                                continue;

                            tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, xLength, yLength);
                            if (pTile.str)
                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, true, true, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedHorTileDraw);
                            else
                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, true, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedHorTileDraw);
                        }

                        for (int currentHorOrdered = 2; currentHorOrdered <= highestHorOrder; currentHorOrdered++)
                            foreach (var pTile in orderedHorTiles)
                            {
                                if (pTile == null)
                                    continue;
                                if (pTile.tileType.orderHor.GetValueOrDefault() != currentHorOrdered)
                                    continue;

                                tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, xLength, yLength);
                                if (pTile.str)
                                    voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, true, true, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedHorTileDraw);
                                else
                                    voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, true, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedHorTileDraw);
                            }

                        currentX = 0;
                        currentY++;

                    }

                    int highestOrder = 1;
                    foreach (var pTile in orderedTiles)
                    {
                        if (pTile == null)
                            continue;
                        if (pTile.tileType.order.GetValueOrDefault() > highestOrder)
                            highestOrder = pTile.tileType.order.GetValueOrDefault();
                        if (pTile.tileType.order.GetValueOrDefault() != 1)
                            continue;

                        tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, xLength, yLength);
                        if (pTile.str)
                            voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, true, false, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedTileDraw);
                        else
                            voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, false, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedTileDraw);
                    }

                    for (int currentOrdered = 2; currentOrdered <= highestOrder; currentOrdered++)
                        foreach (var pTile in orderedTiles)
                        {
                            if (pTile == null)
                                continue;
                            if (pTile.tileType.order.GetValueOrDefault() != currentOrdered)
                                continue;

                            tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, xLength, yLength);
                            if (pTile.str)
                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, true, false, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedTileDraw);
                            else
                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, false, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedTileDraw);
                        }

                    if (mapGamemode != null)
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

                                        tileDrawer.DrawTile(oTile, st.type, options, sizeMultiplier, xLoc, yLoc, xLength, yLength);
                                        voice.Speak(TileActionStringMaker(new TileActionTypes(true, false, false, false, true), oTile, ysLoc, xsLoc, yLength, xLength), ActionType.tileDraw);
                                    }
                                }
                            }
                        }

                    string exportName = options.exportFileName;

                    if (exportName.Contains("?number?"))
                    {
                        string bNumberText = SpaceFiller(bNumber, 4, '0');

                        exportName = exportName.Replace("?number?", bNumberText);
                    }

                    if (options.exportFolderName != null)
                    {
                        if (options.exportFolderName.Trim() != "")
                        {
                            if (batchOption.exportFileName != null)
                            {
                                exportName = batchOption.exportFileName;
                                if (batchOption.exportFileName.Contains("?number?"))
                                {
                                    string bNumberText = SpaceFiller(bNumber, 4, '0');
                                    exportName = exportName.Replace("?number?", bNumberText);
                                }

                                tileDrawer.ExportImage(options, exportName);
                                voice.Speak("[ AAL ] WRITE >> " + options.exportFolderName + "\\" + exportName, ActionType.aal);
                                voice.Speak("\nImage saved to " + Environment.CurrentDirectory + options.exportFolderName + "\\" + exportName + ".", ActionType.saveLocation);
                            }
                            else
                            {
                                tileDrawer.ExportImage(options, exportName);
                                voice.Speak("[ AAL ] WRITE >> " + options.exportFolderName + "\\" + exportName, ActionType.aal);
                                voice.Speak("\nImage saved to " + Environment.CurrentDirectory + options.exportFolderName + "\\" + exportName + ".", ActionType.saveLocation);
                            }
                        }
                        else
                        {
                            if (batchOption.exportFileName != null)
                                tileDrawer.ExportImage(options, batchOption.exportFileName);
                            else
                                tileDrawer.ExportImage(options, exportName);
                            voice.Speak("[ AAL ] WRITE >> " + exportName, ActionType.aal);
                            voice.Speak("\nImage saved to " + Environment.CurrentDirectory + exportName + ".", ActionType.basic);
                        }
                    }
                    else
                    {
                        if (batchOption.exportFileName != null)
                            tileDrawer.ExportImage(options, batchOption.exportFileName);
                        else
                            tileDrawer.ExportImage(options, exportName);
                        voice.Speak("[ AAL ] WRITE >> " + exportName, ActionType.aal);
                        voice.Speak("\nImage saved to " + Environment.CurrentDirectory + exportName + ".", ActionType.basic);
                    }

                }
            }
            catch (Exception e)
            {
                voice.Speak("\nERROR:\n" + e, ActionType.basic);
            }

            voice.Speak("\nFinished.", ActionType.basic);
            voice.Write("log.txt");

            if (oEnd.ToLower() == "pause")
                Console.ReadKey();

        }

        public static string SpaceFiller(string text, int minAmountOfChar, char filler)
        {
            var c = text.ToCharArray();
            string t = "";

            if (c.Length < minAmountOfChar)
            {
                for (int x = 0; x < minAmountOfChar - c.Length; x++)
                    t = filler + t;
                return t + text;
            }
            return text;
        }

        public static string SpaceFiller(int number, int minAmountOfChar, char filler)
        {
            var c = number.ToString().ToCharArray();
            string t = "";

            if (c.Length < minAmountOfChar)
            {
                for (int x = 0; x < minAmountOfChar - c.Length; x++)
                    t = filler + t;
                return t + number;
            }
            return number.ToString();
        }

        public class TileActionTypes
        {
            public TileActionTypes(bool _g, bool _s, bool _o, bool _h, bool _d)
            {
                g = _g;
                s = _s;
                o = _o;
                h = _h;
                d = _d;
            }

            public bool g;
            public bool s;
            public bool o;
            public bool h;
            public bool d;
        }
        
        public class TileDrawer
        {
            Graphics g;
            Bitmap b;

            public TileDrawer(int sizeMultiplier, int horizontalLengthInTiles, int verticalLengthInTiles)
            {
                b = new Bitmap(sizeMultiplier * 2 + sizeMultiplier * horizontalLengthInTiles, sizeMultiplier * 2 + sizeMultiplier * verticalLengthInTiles);
                g = Graphics.FromImage(b);
            }

            public void DrawTile(Tiledata.Tile tile, int type, Options optionsObject, int sizeMultiplier, int currentX, int currentY, int xLength, int yLength)
            {
                var i = SvgDocument.Open("assets\\tiles\\" + optionsObject.preset + "\\" + tile.tileTypes[type - 1].asset);
                var iw = (int)Math.Round(i.Width * sizeMultiplier);
                var ih = (int)Math.Round(i.Height * sizeMultiplier);
                var ihm = (int)Math.Round((double)tile.tileTypes[type - 1].tileParts.top * sizeMultiplier / 1000);
                var iwm = (int)Math.Round((double)tile.tileTypes[type - 1].tileParts.left * sizeMultiplier / 1000);

                g.DrawImage(i.Draw(iw, ih), sizeMultiplier + sizeMultiplier * currentX - iwm, sizeMultiplier + sizeMultiplier * currentY - ihm);
            }

            public void DrawSelectedTile(OrderedTile tile, Options optionsObject, int sizeMultiplier, int xLength, int yLength)
            {
                var i = SvgDocument.Open("assets\\tiles\\" + optionsObject.preset + "\\" + tile.tileType.asset);
                var iw = (int)Math.Round(i.Width * sizeMultiplier);
                var ih = (int)Math.Round(i.Height * sizeMultiplier);
                var ihm = (int)Math.Round((double)tile.tileType.tileParts.top * sizeMultiplier / 1000);
                var iwm = (int)Math.Round((double)tile.tileType.tileParts.left * sizeMultiplier / 1000);

                g.DrawImage(i.Draw(iw, ih), sizeMultiplier + sizeMultiplier * tile.xPosition - iwm, sizeMultiplier + sizeMultiplier * tile.yPosition - ihm);
            }

            public void ColorBackground(Color color1, Color color2, string[] map, Options.BatchSettings batchOption)
            {
                int currentY = 0;
                int currentX = 0;

                foreach (string row in map)
                {

                    foreach (char tile in row.ToCharArray())
                    {
                        if (batchOption.skipTiles.Contains(tile))
                        {
                            currentX++;
                            continue;
                        }

                        if (currentY % 2 == 0)
                        {
                            if (currentX % 2 == 0)
                                g.FillRectangle(new SolidBrush(color1), batchOption.sizeMultiplier + batchOption.sizeMultiplier * currentX, batchOption.sizeMultiplier + batchOption.sizeMultiplier * currentY, batchOption.sizeMultiplier, batchOption.sizeMultiplier);
                            else
                                g.FillRectangle(new SolidBrush(color2), batchOption.sizeMultiplier + batchOption.sizeMultiplier * currentX, batchOption.sizeMultiplier + batchOption.sizeMultiplier * currentY, batchOption.sizeMultiplier, batchOption.sizeMultiplier);
                        }
                        else
                        {
                            if (currentX % 2 == 0)
                                g.FillRectangle(new SolidBrush(color2), batchOption.sizeMultiplier + batchOption.sizeMultiplier * currentX, batchOption.sizeMultiplier + batchOption.sizeMultiplier * currentY, batchOption.sizeMultiplier, batchOption.sizeMultiplier);
                            else
                                g.FillRectangle(new SolidBrush(color1), batchOption.sizeMultiplier + batchOption.sizeMultiplier * currentX, batchOption.sizeMultiplier + batchOption.sizeMultiplier * currentY, batchOption.sizeMultiplier, batchOption.sizeMultiplier);
                        }

                        currentX++;

                    }

                    currentX = 0;
                    currentY++;

                }
            }

            public void ExportImage(Options optionsObject, string fileName)
            {
                if (!Directory.Exists(optionsObject.exportFolderName))
                    Directory.CreateDirectory(optionsObject.exportFolderName);
                b.Save(optionsObject.exportFolderName + "\\" + fileName, ImageFormat.Png);
            }
        }

        public static string TileActionStringMaker(TileActionTypes tat, Tiledata.Tile tile, int yLocation, int xLocation, int yLocationMax, int xLocationMax)
        {
            string p;
            string t;

            if (tat.g) p = "g"; else p = " ";
            if (tat.s) p = p + "s"; else p = p + " ";
            if (tat.o) p = p + "o"; else p = p + " ";
            if (tat.h) p = p + "h"; else p = p + " ";
            if (tat.d) p = p + "d"; else p = p + " ";

            if (tat.g)
                t = "DRAWN AS \"" + tile.tileName + "\".";
            else if (tat.s)
            {
                if (tat.o)
                {
                    if (tat.h)
                    {
                        if (tat.d)
                            t = "DRAWN HORIZONTALLY ORDERED TILE AS \"" + tile.tileName + "\" (SPECIAL TILE RULES).";
                        else
                            t = "\"" + tile.tileName + "\" DELAYED FOR HORIZONTAL ORDERING (SPECIAL TILE RULES).";
                    }
                    else
                    {
                        if (tat.d)
                            t = "DRAWN ORDERED TILE AS \"" + tile.tileName + "\" (SPECIAL TILE RULES).";
                        else
                            t = "\"" + tile.tileName + "\" DELAYED FOR ORDERING (SPECIAL TILE RULES).";
                    }
                }
                else
                {
                    if (tat.d)
                        t = "DRAWN AS \"" + tile.tileName + "\" (SPECIAL TILE RULES).";
                    else
                        t = "SKIPPED.";
                }
            }
            else if (tat.o)
            {
                if (tat.h)
                {
                    if (tat.d)
                        t = "DRAWN HORIZONTALLY ORDERED TILE AS \"" + tile.tileName + "\".";
                    else
                        t = "\"" + tile.tileName + "\" DELAYED FOR HORIZONTAL ORDERING.";
                }
                else
                {
                    if (tat.d)
                        t = "DRAWN ORDERED TILE AS \"" + tile.tileName + "\".";
                    else
                        t = "\"" + tile.tileName + "\" DELAYED FOR ORDERING.";
                }
            }
            else
                t = "DRAWN AS \"" + tile.tileName + "\"";

            return p + " [" + tile.tileCode + "] < y: " + SpaceFiller(yLocation, yLocationMax.ToString().ToCharArray().Length, ' ') + " / x: " + SpaceFiller(xLocation, xLocationMax.ToString().ToCharArray().Length, ' ') + " > " + t;
        }
        public static string TileActionStringMaker(TileActionTypes tat, Tiledata.Tile tile, string yLocation, string xLocation, int yLocationMax, int xLocationMax)
        {
            string p;
            string t;

            if (tat.g) p = "g"; else p = " ";
            if (tat.s) p = p + "s"; else p = p + " ";
            if (tat.o) p = p + "o"; else p = p + " ";
            if (tat.h) p = p + "h"; else p = p + " ";
            if (tat.d) p = p + "d"; else p = p + " ";

            if (tat.g)
                t = "DRAWN AS \"" + tile.tileName + "\".";
            else if (tat.s)
            {
                if (tat.o)
                {
                    if (tat.h)
                    {
                        if (tat.d)
                            t = "DRAWN HORIZONTALLY ORDERED TILE AS \"" + tile.tileName.ToUpper() + "\" (SPECIAL TILE RULES).";
                        else
                            t = "\"" + tile.tileName.ToUpper() + "\" DELAYED FOR HORIZONTAL ORDERING (SPECIAL TILE RULES).";
                    }
                    else
                    {
                        if (tat.d)
                            t = "DRAWN ORDERED TILE AS \"" + tile.tileName.ToUpper() + "\" (SPECIAL TILE RULES).";
                        else
                            t = "\"" + tile.tileName.ToUpper() + "\" DELAYED FOR ORDERING (SPECIAL TILE RULES).";
                    }
                }
                else
                {
                    if (tat.d)
                        t = "DRAWN AS \"" + tile.tileName.ToUpper() + "\" (SPECIAL TILE RULES).";
                    else
                        t = "SKIPPED.";
                }
            }
            else if (tat.o)
            {
                if (tat.h)
                {
                    if (tat.d)
                        t = "DRAWN HORIZONTALLY ORDERED TILE AS \"" + tile.tileName.ToUpper() + "\".";
                    else
                        t = "\"" + tile.tileName.ToUpper() + "\" DELAYED FOR HORIZONTAL ORDERING.";
                }
                else
                {
                    if (tat.d)
                        t = "DRAWN ORDERED TILE AS \"" + tile.tileName.ToUpper() + "\".";
                    else
                        t = "\"" + tile.tileName.ToUpper() + "\" DELAYED FOR ORDERING.";
                }
            }
            else
                t = "DRAWN AS \"" + tile.tileName.ToUpper() + "\"";

            return p + " [" + tile.tileCode + "] < y: " + SpaceFiller(yLocation, yLocationMax.ToString().ToCharArray().Length, ' ') + " / x: " + SpaceFiller(xLocation, xLocationMax.ToString().ToCharArray().Length, ' ') + " > " + t;
        }

        public enum ActionType { setup, tileDraw, orderedHorTileDraw, orderedTileDraw, saveLocation, aal, basic }
        public class Voice
        {
            Options savedOptionsObject;

            public Voice()
            {
                savedOptionsObject = new Options { console = new Options.ConsoleOptions() { aal = true, orderedHorTileDraw = true, orderedTileDraw = true, saveLocation = true, setup = true, tileDraw = true }, saveLogFile = true };
            }

            public void UpdateOptions(Options optionsObject)
            {
                savedOptionsObject = optionsObject;
            }

            List<string> loggedLines = new List<string>();

            public void Speak(string text, ActionType actionType)
            {
                switch (actionType)
                {
                    case ActionType.setup:
                        if (savedOptionsObject.console.setup)
                        {
                            Console.WriteLine(text);
                            loggedLines.Add(text);
                        }
                        break;
                    case ActionType.tileDraw:
                        if (savedOptionsObject.console.tileDraw)
                        {
                            Console.WriteLine(text);
                            loggedLines.Add(text);
                        }
                        break;
                    case ActionType.orderedHorTileDraw:
                        if (savedOptionsObject.console.orderedHorTileDraw)
                        {
                            Console.WriteLine(text);
                            loggedLines.Add(text);
                        }
                        break;
                    case ActionType.orderedTileDraw:
                        if (savedOptionsObject.console.orderedTileDraw)
                        {
                            Console.WriteLine(text);
                            loggedLines.Add(text);
                        }
                        break;
                    case ActionType.saveLocation:
                        if (savedOptionsObject.console.saveLocation)
                        {
                            Console.WriteLine(text);
                            loggedLines.Add(text);
                        }
                        break;
                    case ActionType.aal:
                        if (savedOptionsObject.console.aal)
                        {
                            Console.WriteLine(text);
                            loggedLines.Add(text);
                        }
                        break;
                    case ActionType.basic:
                        Console.WriteLine(text);
                        loggedLines.Add(text);
                        break;
                }
            }

            public void Write(string fileName)
            {
                if (savedOptionsObject.saveLogFile)
                {
                    if (savedOptionsObject.console.aal)
                        Console.WriteLine("[ AAL ] WRITE >> " + fileName + "");
                    File.WriteAllText(fileName, string.Join("\n", loggedLines).Replace("\n", Environment.NewLine));
                    Console.WriteLine("\nLog saved to log.txt");
                }
                else
                    Console.WriteLine("\nLog saving is disabled.");
            }
        }

    }

}
