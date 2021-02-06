using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BMG
{
    public class Program
    {
        static void Main(string[] args)
        {
            var fArgs = " " + string.Join(" ", args);

            int major = 1;
            int minor = 8;
            int patch = 2; 
            string access = "Release";
            string oLoc = "options.json";
            string oStr = "";
            string oEnd = "";

            // Fun statistical ending numbers
            int tilesDrawn = 0;
            List<char> tilesFailedChars = new List<char>();
            Dictionary<char, int> tilesFailed = new Dictionary<char, int>();

            Options options = new Options();
            Voice voice = new Voice();

            Stopwatch stopwatch = new Stopwatch();

            try
            {
                stopwatch.Start();

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

                if (fArgs.ToLower().Contains(" -e ")) // Ending type
                {
                    oEnd = fArgs.Replace(" -e ", "@").Replace(" -E ", "@").Split('@')[1].Trim();
                    if (oEnd.Contains(" -"))
                        oEnd = oEnd.Replace(" -", "#").Split('#')[0].Trim();
                    else
                        oEnd = oEnd.Trim();
                }

                if (oStr == "")
                {
                    if (!File.Exists(oLoc)) // Check if file exists
                    {
                        voice.Speak("\n[Forced] Status: ERROR!\n  Error reason:\n  Option file doesn't exist\n  [FileReader] Unable to find file in location \"" + oLoc + "\"", ActionType.basic);
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

                voice.UpdateOptions(options, major + "." + minor + "." + patch);

                if (options.setPath != null)
                    Environment.CurrentDirectory = options.setPath;

                voice.Speak("\n  BMG (Brawl Map Gen)\n    Version: v" + major + "." + minor + "." + patch + " " + access + "\n    Created by: RedH1ghway (aka BloodWiing)\n    Helped by: 4JR, Henry, tryso\n\n", ActionType.basic);
                voice.Speak(" Status: App is launched!", ActionType.statusChange);
                voice.Speak("Loading preset: \"" + options.preset.ToUpper() + "\"...", ActionType.setup);
                voice.Speak("[ AAL ] READ << ./presets/" + options.preset + ".json", ActionType.aal);

                voice.Title.Job.UpdateJob(0, 1, "Preparing...");
                voice.Title.Status.UpdateStatus(0, 1, "Loading preset...");
                voice.Title.UpdateStatusDetails(options.preset.ToUpper(), Voice.TitleClass.StatusDetailsType.basic);
                voice.Title.RefreshTitle();

                if (!File.Exists("./presets/" + options.preset + ".json"))
                {
                    voice.Speak("\n [Forced] Status: ERROR!\n  Error reason:\n  Preset doesn't exist\n  [FileReader] Unable to find file in location \"presets/" + options.preset + ".json\"", ActionType.basic);
                    voice.Write("log.txt");
                    Thread.Sleep(3000);
                    Environment.Exit(1);
                }

                StreamReader r2 = new StreamReader("./presets/" + options.preset + ".json");
                string json2 = r2.ReadToEnd();
                voice.Speak("Preset loaded: \"" + options.preset.ToUpper() + "\"!", ActionType.setup);
                voice.Speak(" Status: Tiles will be drawn from preset \"" + options.preset.ToUpper() + "\".", ActionType.statusChange);
                var tiledata = JsonConvert.DeserializeObject<Tiledata>(json2);
                Dictionary<int, SavedImages> savedTileImageList = new Dictionary<int, SavedImages>();

                int totalSizes, totalImages = 0;

                {  // Local calculations
                    List<int> sizes = new List<int>();
                    foreach (Options.BatchSettings single in options.batch)
                        if (!sizes.Contains(single.sizeMultiplier))
                            sizes.Add(single.sizeMultiplier);
                    totalSizes = sizes.Count;
                }

                foreach (string folder in Directory.GetDirectories("./assets/tiles/" + options.preset + "/"))
                    totalImages += Directory.GetFiles(folder).Length;
                totalImages += Directory.GetFiles("./assets/tiles/" + options.preset + "/").Length;

                voice.Title.Job.UpdateJob(0, totalSizes, "Preloading tiles...");
                voice.Title.RefreshTitle();
                voice.Speak("\n Status: Tile Preloading started.", ActionType.setup);
                foreach (Options.BatchSettings single in options.batch) // Tile preloader
                {
                    if (savedTileImageList.ContainsKey(single.sizeMultiplier))
                        continue;

                    voice.Title.Job.IncreaseJob();
                    voice.Title.Status.UpdateStatus(0, totalImages, "Reading...");
                    voice.Title.RefreshTitle();

                    voice.Speak("Found new tilesize.", ActionType.setup);
                    savedTileImageList.Add(single.sizeMultiplier, new SavedImages(options, tiledata.tiles, single.sizeMultiplier, voice)); // Preload tiles for a specific tiles
                }
                voice.Speak("\n Status: Tile Preload complete.", ActionType.statusChange);
                voice.Speak("Preloaded tiles with tilesizes:", ActionType.setup);
                foreach (var si in savedTileImageList)
                    voice.Speak("  " + si.Key + "px", ActionType.setup);

                int bNumber = 0;
                voice.Speak("\n Status: Map image generator starting...", ActionType.statusChange);
                voice.Title.Job.UpdateJob(0, options.batch.Length, "\"temp_name\"");
                foreach (var batchOption in options.batch)
                {
                    voice.Title.Job.UpdateJobName(("\"" + batchOption.name + "\"").Replace("\"?number?\"", "Number " + bNumber));
                    voice.Title.Job.IncreaseJob();

                    SavedImages selectedTileImageList = savedTileImageList[batchOption.sizeMultiplier];

                    bNumber++;

                    voice.Speak("\n Status: Getting map...", ActionType.statusChange);
                    voice.Speak("Looking for map number " + bNumber + "...", ActionType.setup);

                    var map = batchOption.map;
                    var sizeMultiplier = batchOption.sizeMultiplier;

                    if (map == null)
                    {
                        voice.Speak("\n [Forced] Status: Warning!\n  Warning details:\n  Map is empty!\n  [Object] Map in the index number " + bNumber + " is not defined.", ActionType.basic);
                        Thread.Sleep(3000);
                        continue;
                    }
                    else if (map.Length == 0)
                    {
                        voice.Speak("\n [Forced] Status: Warning!\n  Warning details:\n  Map is empty!\n  [Object] Map in the index number " + bNumber + " has no string arrays.", ActionType.basic);
                        Thread.Sleep(3000);
                        continue;
                    }

                    // Preparing and drawing background
                    int xLength = map[0].Length;
                    int yLength = map.Length;

                    Tiledata.Biome mapBiome = tiledata.GetBiome(batchOption.biome - 1);

                    voice.Speak("  Map found.", ActionType.setup);
                    voice.Speak(" Status: Map gotten.", ActionType.statusChange);
                    voice.Speak("\nMap details:\n  Width: " + (sizeMultiplier * 2 + sizeMultiplier * xLength) + "px\n  Height: " + (sizeMultiplier * 2 + sizeMultiplier * yLength) + "px\n  Biome: \"" + mapBiome.name.ToUpper() + "\"\n", ActionType.setup);

                    float[] border = emptyBorderAmoutNormalizer(batchOption.emptyBorderAmount);

                    TileDrawer tileDrawer = new TileDrawer(batchOption.sizeMultiplier, batchOption.map[0].Length, batchOption.map.Length, border);

                    int currentY = 0;
                    int currentX = 0;

                    voice.Speak("Coloring background...", ActionType.setup);
                    voice.Speak(" Status: Fetching tile colors...", ActionType.statusChange);

                    string[] color1s = mapBiome.color1.Split(',');
                    Color color1 = Color.FromArgb(int.Parse(color1s[0].Trim()), int.Parse(color1s[1].Trim()), int.Parse(color1s[2].Trim()));

                    string[] color2s = mapBiome.color2.Split(',');
                    Color color2 = Color.FromArgb(int.Parse(color2s[0].Trim()), int.Parse(color2s[1].Trim()), int.Parse(color2s[2].Trim()));

                    voice.Speak(" Status: Colors fetched.", ActionType.statusChange);
                    voice.Speak(" Status: Coloring background...", ActionType.statusChange);

                    tileDrawer.ColorBackground(color1, color2, map, batchOption, border);
                    voice.Speak(" Status: Background colored.", ActionType.statusChange);

                    List<OrderedTile> orderedTiles = new List<OrderedTile>();

                    voice.Speak("Drawing map tiles...", ActionType.setup);
                    if (batchOption.name != "?number?")
                        voice.Speak(" Status: Drawing map (\"" + batchOption.name + "\")...", ActionType.statusChange);
                    else
                        voice.Speak(" Status: Drawing map (#" + bNumber + ")...", ActionType.statusChange);

                    Tiledata.Gamemode mapGamemode = null;
                    foreach (var gm in tiledata.gamemodes)

                    {
                        if (gm == null || batchOption.gamemode == null)
                            break;
                        if (gm.name == batchOption.gamemode)
                            mapGamemode = gm;
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

                                            tileDrawer.DrawTile(oTile, st.type, options, sizeMultiplier, xLoc, yLoc, xLength, yLength, selectedTileImageList, border);
                                            tilesDrawn++;
                                            voice.Speak(TileActionStringMaker(new TileActionTypes(true, false, false, false, true), oTile, ysLoc, xsLoc, yLength, xLength), ActionType.tileDraw);
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

                                        var row = map[yLoc].ToCharArray();
                                        row[xLoc] = oTile.tileCode;
                                        map[yLoc] = string.Join("", row);

                                        voice.Speak(TileActionStringMaker(new TileActionTypes(true, false, true, false, false), oTile, ysLoc, xsLoc, yLength, xLength), ActionType.gamemodeModding);
                                    }

                    }

                    Options.SpecialTileRules[] str = null;
                    if (batchOption.specialTileRules != null)
                        str = batchOption.specialTileRules;

                    List<Options.RecordedSTR> rstr = new List<Options.RecordedSTR>();

                    voice.Title.Status.UpdateStatus(0, map.Length * map[0].Length, "Drawing tiles...");
                    // Begin to draw map
                    foreach (string row in map)
                    {
                        List<OrderedTile> orderedHorTiles = new List<OrderedTile>();

                        foreach (char tTile in row.ToCharArray())
                        {
                            voice.Title.Status.IncreaseStatus();
                            voice.Title.RefreshTitle();

                            bool tileDrawn = false;

                            var tile = tTile;

                            foreach (Options.Replace repTile in batchOption.replaceTiles) // Specified Tile Code Replacer
                            {
                                if (tile == repTile.from)
                                    tile = repTile.to;
                            }

                            if (batchOption.skipTiles.Contains(tile)) // Specified Tile Skipper
                            {
                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, false, false, false), new Tiledata.Tile() { tileName = "", tileCode = tile }, currentY, currentX, yLength, xLength), ActionType.tileDraw);
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
                                        Options.RecordRSTR(rstr, tile);
                                        foreach (var orstr in rstr)
                                            if (orstr.tileCode == tile)
                                                if (ostr.tileTime == orstr.tileTime)
                                                    foreach (var aTile in tiledata.tiles)
                                                        if (aTile.tileCode == tile)
                                                        {
                                                            // Save tile for later drawing (Ordering and Horizontal Ordering)
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

                                                            // Draw STR Tile
                                                            tileDrawer.DrawTile(aTile, ostr.tileType, options, sizeMultiplier, currentX, currentY, xLength, yLength, selectedTileImageList, border);
                                                            tilesDrawn++;
                                                            voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, false, false, true), aTile, currentY, currentX, yLength, xLength), ActionType.tileDraw);

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

                                        if (batchOption.gamemode != null) // Biome overrider (from Gamemode options)
                                            if (mapGamemode != null)
                                                if (mapGamemode.overrideBiome != null)
                                                    if (mapGamemode.name == batchOption.gamemode)
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
                                                NeighborBinary = tileLinks(map, currentX, currentY, aTile, batchOption.replaceTiles);

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
                                                    voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, false, false), aTile, currentY, currentX, yLength, xLength), ActionType.orderedTileDraw);
                                                    orderedTiles.Add(new OrderedTile()
                                                    {
                                                        tileType = breakerTile,
                                                        xPosition = currentX,
                                                        yPosition = currentY,
                                                        tileCode = aTile.tileCode,
                                                        tileName = aTile.tileName
                                                    });
                                                    tileDrawn = true;
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
                                                    tileDrawn = true;
                                                    break;
                                                }

                                                // Draw Tile
                                                tileDrawer.DrawSelectedTile(new OrderedTile() { tileType = breakerTile, xPosition = currentX, yPosition = currentY, tileCode = aTile.tileCode, tileName = aTile.tileName }, options, sizeMultiplier, xLength, yLength, selectedTileImageList, border);
                                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, false, false, true), aTile, currentY, currentX, yLength, xLength), ActionType.tileDraw);
                                                tileDrawn = true;
                                                break;
                                            }

                                            // Save tile for later drawing (Ordering and Horizontal Ordering)
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
                                                tileDrawn = true;
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
                                                tileDrawn = true;
                                                break;
                                            }

                                            // Draw Tile
                                            tileDrawer.DrawTile(aTile, setTileDefault.type, options, sizeMultiplier, currentX, currentY, xLength, yLength, selectedTileImageList, border);
                                            tilesDrawn++;
                                            voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, false, false, true), aTile, currentY, currentX, yLength, xLength), ActionType.tileDraw);
                                            tileDrawn = true;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (!tileDrawn)
                            {
                                if (!tilesFailedChars.Contains(tTile))
                                {
                                    tilesFailed.Add(tTile, 1);
                                    tilesFailedChars.Add(tTile);
                                }
                                else
                                    tilesFailed[tTile]++;
                            }

                            currentX++;

                        }

                        // Draw Horizontally Ordered Tiles
                        int highestHorOrder = 1;
                        foreach (var pTile in orderedHorTiles)
                        {
                            if (pTile == null)
                                continue;
                            if (pTile.tileType.orderHor.GetValueOrDefault() > highestHorOrder)
                                highestHorOrder = pTile.tileType.orderHor.GetValueOrDefault();
                            if (pTile.tileType.orderHor.GetValueOrDefault() != 1)
                                continue;

                            tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, xLength, yLength, selectedTileImageList, border);
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

                                tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, xLength, yLength, selectedTileImageList, border);
                                if (pTile.str)
                                    voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, true, true, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedHorTileDraw);
                                else
                                    voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, true, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedHorTileDraw);
                            }

                        currentX = 0;
                        currentY++;

                    }

                    // Draw Ordered Tiles
                    int highestOrder = 1;
                    foreach (var pTile in orderedTiles)
                    {
                        if (pTile == null)
                            continue;
                        if (pTile.tileType.order.GetValueOrDefault() > highestOrder)
                            highestOrder = pTile.tileType.order.GetValueOrDefault();
                        if (pTile.tileType.order.GetValueOrDefault() != 1)
                            continue;

                        tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, xLength, yLength, selectedTileImageList, border);
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

                            tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, xLength, yLength, selectedTileImageList, border);
                            if (pTile.str)
                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, true, true, false, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedTileDraw);
                            else
                                voice.Speak(TileActionStringMaker(new TileActionTypes(false, false, true, false, true), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength), ActionType.orderedTileDraw);
                        }

                    if (mapGamemode != null) // Draw Gamemode tiles (after everything else)
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

                                            tileDrawer.DrawTile(oTile, st.type, options, sizeMultiplier, xLoc, yLoc, xLength, yLength, selectedTileImageList, border);
                                            tilesDrawn++;
                                            voice.Speak(TileActionStringMaker(new TileActionTypes(true, false, false, false, true), oTile, ysLoc, xsLoc, yLength, xLength), ActionType.tileDraw);
                                        }
                                    }
                                }
                            }

                    string exportName = options.exportFileName;

                    if (exportName.Contains("?number?"))
                    {
                        string bNumberText = LeftSpaceFiller(bNumber, 4, '0');

                        exportName = exportName.Replace("?number?", bNumberText);
                    }

                    // Save map image
                    voice.Speak(" Status: Map drawn.", ActionType.statusChange);
                    if (options.exportFolderName != null)
                    {
                        if (options.exportFolderName.Trim() != "")
                        {
                            if (batchOption.exportFileName != null)
                            {
                                exportName = batchOption.exportFileName;
                                if (batchOption.exportFileName.Contains("?number?"))
                                {
                                    string bNumberText = LeftSpaceFiller(bNumber, 4, '0');
                                    exportName = exportName.Replace("?number?", bNumberText);
                                }

                                tileDrawer.ExportImage(options, exportName);
                                voice.Speak("[ AAL ] WRITE >> " + options.exportFolderName + "/" + exportName, ActionType.aal);
                                if (Regex.IsMatch(options.exportFolderName, "\\S:"))
                                    voice.Speak("\nImage saved!\n  Location: " + options.exportFolderName + "/" + exportName, ActionType.basic);
                                else
                                    voice.Speak("\nImage saved!\n  Location: " + Path.GetFullPath("./" + options.exportFolderName + "/" + exportName), ActionType.saveLocation);
                            }
                            else
                            {
                                tileDrawer.ExportImage(options, exportName);
                                voice.Speak("[ AAL ] WRITE >> " + options.exportFolderName + "/" + exportName, ActionType.aal);
                                if (Regex.IsMatch(options.exportFolderName, "\\S:"))
                                    voice.Speak("\nImage saved!\n  Location: " + options.exportFolderName + "/" + exportName, ActionType.basic);
                                else
                                    voice.Speak("\nImage saved!\n  Location: " + Path.GetFullPath("./" + options.exportFolderName + "/" + exportName), ActionType.saveLocation);
                            }
                        }
                        else
                        {
                            if (batchOption.exportFileName != null)
                                tileDrawer.ExportImage(options, batchOption.exportFileName);
                            else
                                tileDrawer.ExportImage(options, exportName);
                            voice.Speak("[ AAL ] WRITE >> " + exportName, ActionType.aal);
                            if (Regex.IsMatch(exportName, "\\S:"))
                                voice.Speak("\nImage saved!\n  Location: " + exportName, ActionType.basic);
                            else
                                voice.Speak("\nImage saved!\n  Location: " + Path.GetFullPath("./" + exportName), ActionType.basic);
                        }
                    }
                    else
                    {
                        if (batchOption.exportFileName != null)
                            tileDrawer.ExportImage(options, batchOption.exportFileName);
                        else
                            tileDrawer.ExportImage(options, exportName);
                        voice.Speak("[ AAL ] WRITE >> " + exportName, ActionType.aal);
                        if (Regex.IsMatch(exportName, "\\S:"))
                            voice.Speak("\nImage saved!\n  Location: " + exportName, ActionType.basic);
                        else
                            voice.Speak("\nImage saved!\n  Location: " + Path.GetFullPath("./" + exportName), ActionType.basic);
                    }
                }
            }
            catch (Exception e)
            {
                voice.Speak("\n [Forced] Status: ERROR!\n  Error reason:\n  " + e, ActionType.basic);
            }

            stopwatch.Stop();
            voice.Speak("\nFinished.", ActionType.basic);

            string stTime = "";
            if (stopwatch.ElapsedMilliseconds / 86400000 != 0)
                stTime = (stopwatch.ElapsedMilliseconds / 86400000) + "d " + (stopwatch.ElapsedMilliseconds / 3600000 - stopwatch.ElapsedMilliseconds / 86400000 * 24) + "h " + (stopwatch.ElapsedMilliseconds / 60000 - stopwatch.ElapsedMilliseconds / 3600000 * 60) + "m " + (stopwatch.ElapsedMilliseconds / 1000 - stopwatch.ElapsedMilliseconds / 60000 * 60) + "s " + (stopwatch.ElapsedMilliseconds - stopwatch.ElapsedMilliseconds / 1000 * 1000) + "ms";
            else if (stopwatch.ElapsedMilliseconds / 3600000 != 0)
                stTime = (stopwatch.ElapsedMilliseconds / 3600000) + "h " + (stopwatch.ElapsedMilliseconds / 60000 - stopwatch.ElapsedMilliseconds / 3600000 * 60) + "m " + (stopwatch.ElapsedMilliseconds / 1000 - stopwatch.ElapsedMilliseconds / 60000 * 60) + "s " + (stopwatch.ElapsedMilliseconds - stopwatch.ElapsedMilliseconds / 1000 * 1000) + "ms";
            else if (stopwatch.ElapsedMilliseconds / 60000 != 0)
                stTime = (stopwatch.ElapsedMilliseconds / 60000) + "m " + (stopwatch.ElapsedMilliseconds / 1000 - stopwatch.ElapsedMilliseconds / 60000 * 60) + "s " + (stopwatch.ElapsedMilliseconds - stopwatch.ElapsedMilliseconds / 1000 * 1000) + "ms";
            else if (stopwatch.ElapsedMilliseconds / 1000 != 0)
                stTime = (stopwatch.ElapsedMilliseconds / 1000) + "s " + (stopwatch.ElapsedMilliseconds - stopwatch.ElapsedMilliseconds / 1000 * 1000) + "ms";
            else
                stTime = stopwatch.ElapsedMilliseconds + "ms";

            voice.Speak("\nResults:\n  Total Maps Drawn: " + options.batch.Length + "\n  Total Tiles Drawn: " + tilesDrawn + "\n  Completed in: " + stTime, ActionType.basic);

            if (tilesFailed.Count == 0)
                voice.Speak("  No unrecognized tiles encountered.", ActionType.basic);
            else
            {
                voice.Speak("  Unrecognized tiles encountered:", ActionType.basic);
                foreach (var t in tilesFailedChars)
                    voice.Speak("    \"" + t + "\": " + tilesFailed[t], ActionType.basic);
            }
            voice.Speak("", ActionType.basic);

            voice.Write("log.txt");

            Console.ReadKey();

            if (oEnd.ToLower() == "pause")
                Console.ReadKey();

        }

        public static string LeftSpaceFiller(string text, int minAmountOfChar, char filler) // AMTool: Make text more sylish by filling in empty spaces with a selected character to make up for it
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

        public static string LeftSpaceFiller(int number, int minAmountOfChar, char filler) // AMTool: Make text more sylish by filling in empty spaces with a selected character to make up for it
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

        public static string RightSpaceFiller(string text, int minAmountOfChar, char filler) // AMTool: Make text more sylish by filling in empty spaces with a selected character to make up for it
        {
            var c = text.ToCharArray();
            string t = "";

            if (c.Length < minAmountOfChar)
            {
                for (int x = 0; x < minAmountOfChar - c.Length; x++)
                    t = filler + t;
                return text + t;
            }
            return text;
        }

        public static string RightSpaceFiller(int number, int minAmountOfChar, char filler) // AMTool: Make text more sylish by filling in empty spaces with a selected character to make up for it
        {
            var c = number.ToString().ToCharArray();
            string t = "";

            if (c.Length < minAmountOfChar)
            {
                for (int x = 0; x < minAmountOfChar - c.Length; x++)
                    t = filler + t;
                return number + t;
            }
            return number.ToString();
        }

        public static string MakeProgressBar(int lenght, char fill, char background, double percent, bool leftToRight = true) // AMTool: Text progress bars
        {
            string prog = "";
            for (int x = 0; x < Math.Floor(lenght * percent); x++)
                prog += fill;
            if (leftToRight)
                return RightSpaceFiller(prog, lenght, background);
            else
                return LeftSpaceFiller(prog, lenght, background);   
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

            public TileDrawer(int sizeMultiplier, int horizontalLengthInTiles, int verticalLengthInTiles, float[] borderSize)
            {
                b = new Bitmap((int)Math.Round(sizeMultiplier * (borderSize[2] + borderSize[3] + horizontalLengthInTiles)), (int)Math.Round(sizeMultiplier * (borderSize[0] + borderSize[1] + verticalLengthInTiles)));
                g = Graphics.FromImage(b);
            }

            public void DrawTile(Tiledata.Tile tile, int type, Options optionsObject, int sizeMultiplier, int currentX, int currentY, int xLength, int yLength, SavedImages imageMemory, float[] borderSize) // Drawing a tile (normal)
            {
                foreach (SavedImages.TileImage ti in imageMemory.tileImages)
                {
                    if (ti.imageName == tile.tileTypes[type - 1].asset)
                    {
                        g.DrawImage(ti.renderedImage, (int)Math.Round(sizeMultiplier * (currentX + borderSize[2])) - ti.imageOffsetLeft, (int)Math.Round(sizeMultiplier * (currentY + borderSize[0])) - ti.imageOffsetTop);
                        return;
                    }
                }
            }

            public void DrawSelectedTile(OrderedTile tile, Options optionsObject, int sizeMultiplier, int xLength, int yLength, SavedImages imageMemory, float[] borderSize) // Drawing a tile (with saved coordinates and a pre-selected type)
            {
                foreach (SavedImages.TileImage ti in imageMemory.tileImages)
                {
                    if (ti.imageName == tile.tileType.asset)
                    {
                        g.DrawImage(ti.renderedImage, (int)Math.Round(sizeMultiplier * (tile.xPosition + borderSize[2])) - ti.imageOffsetLeft, (int)Math.Round(sizeMultiplier * (tile.yPosition + borderSize[0])) - ti.imageOffsetTop);
                        return;
                    }
                }
            }

            public void ColorBackground(Color color1, Color color2, string[] map, Options.BatchSettings batchOption, float[] borderSize) // Filling in background colors
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
                                g.FillRectangle(
                                    new SolidBrush(color1),
                                    (int)Math.Round(batchOption.sizeMultiplier * (currentX + borderSize[2])),
                                    (int)Math.Round(batchOption.sizeMultiplier * (currentY + borderSize[0])),
                                    batchOption.sizeMultiplier, batchOption.sizeMultiplier);
                            else
                                g.FillRectangle(
                                    new SolidBrush(color2),
                                    (int)Math.Round(batchOption.sizeMultiplier * (currentX + borderSize[2])),
                                    (int)Math.Round(batchOption.sizeMultiplier * (currentY + borderSize[0])),
                                    batchOption.sizeMultiplier, batchOption.sizeMultiplier);
                        }
                        else
                        {
                            if (currentX % 2 == 0)
                                g.FillRectangle(
                                    new SolidBrush(color2),
                                    (int)Math.Round(batchOption.sizeMultiplier * (currentX + borderSize[2])),
                                    (int)Math.Round(batchOption.sizeMultiplier * (currentY + borderSize[0])),
                                    batchOption.sizeMultiplier, batchOption.sizeMultiplier);
                            else
                                g.FillRectangle(
                                    new SolidBrush(color1),
                                    (int)Math.Round(batchOption.sizeMultiplier * (currentX + borderSize[2])),
                                    (int)Math.Round(batchOption.sizeMultiplier * (currentY + borderSize[0])),
                                    batchOption.sizeMultiplier, batchOption.sizeMultiplier);
                        }

                        currentX++;

                    }

                    currentX = 0;
                    currentY++;

                }
            }

            public void ExportImage(Options optionsObject, string fileName) // Saving the generated image
            {
                if (!Directory.Exists(optionsObject.exportFolderName))
                    Directory.CreateDirectory(optionsObject.exportFolderName);
                if (Regex.IsMatch(fileName, "\\S:"))
                    b.Save(fileName, ImageFormat.Png);
                else
                    b.Save(optionsObject.exportFolderName + "/" + fileName, ImageFormat.Png);

                b.Dispose();
                g.Dispose();
            }
        }
        
        public static string TileActionStringMaker(TileActionTypes tat, Tiledata.Tile tile, int yLocation, int xLocation, int yLocationMax, int xLocationMax) // Text maker for a voice when the generator is doing actions related to tiles
        {
            string p;
            string t;
            string n = tile.tileName.ToUpper();

            if (tat.g) p = "g"; else p = " ";
            if (tat.s) p += "s"; else p += " ";
            if (tat.o) p += "o"; else p += " ";
            if (tat.h) p += "h"; else p += " ";
            if (tat.d) p += "d"; else p += " ";

            if (tat.g)
                t = "DRAWN AS \"" + n + "\".";
            else if (tat.s)
            {
                if (tat.o)
                {
                    if (tat.h)
                    {
                        if (tat.d)
                            t = "DRAWN HORIZONTALLY ORDERED TILE AS \"" + n + "\" (SPECIAL TILE RULES).";
                        else
                            t = "\"" + n + "\" DELAYED FOR HORIZONTAL ORDERING (SPECIAL TILE RULES).";
                    }
                    else
                    {
                        if (tat.d)
                            t = "DRAWN ORDERED TILE AS \"" + n + "\" (SPECIAL TILE RULES).";
                        else
                            t = "\"" + n + "\" DELAYED FOR ORDERING (SPECIAL TILE RULES).";
                    }
                }
                else
                {
                    if (tat.d)
                        t = "DRAWN AS \"" + n + "\" (SPECIAL TILE RULES).";
                    else
                        t = "SKIPPED.";
                }
            }
            else if (tat.o)
            {
                if (tat.h)
                {
                    if (tat.d)
                        t = "DRAWN HORIZONTALLY ORDERED TILE AS \"" + n + "\".";
                    else
                        t = "\"" + n + "\" DELAYED FOR HORIZONTAL ORDERING.";
                }
                else
                {
                    if (tat.d)
                        t = "DRAWN ORDERED TILE AS \"" + n + "\".";
                    else
                        t = "\"" + n + "\" DELAYED FOR ORDERING.";
                }
            }
            else
                t = "DRAWN AS \"" + n + "\"";

            return p + " [" + tile.tileCode + "] < y: " + LeftSpaceFiller(yLocation, yLocationMax.ToString().ToCharArray().Length, ' ') + " / x: " + LeftSpaceFiller(xLocation, xLocationMax.ToString().ToCharArray().Length, ' ') + " > " + t;
        }
        public static string TileActionStringMaker(TileActionTypes tat, Tiledata.Tile tile, string yLocation, string xLocation, int yLocationMax, int xLocationMax) // Text maker for a voice when the generator is doing actions related to tiles
        {
            string p;
            string t;
            string n = tile.tileName.ToUpper();

            if (tat.g) p = "g"; else p = " ";
            if (tat.s) p += "s"; else p += " ";
            if (tat.o) p += "o"; else p += " ";
            if (tat.h) p += "h"; else p += " ";
            if (tat.d) p += "d"; else p += " ";

            if (tat.g)
            {
                if (tat.o)
                    t = "MODIFIED TO \"" + n + "\".";
                else
                    t = "DRAWN AS \"" + n + "\".";
            }
            else if (tat.s)
            {
                if (tat.o)
                {
                    if (tat.h)
                    {
                        if (tat.d)
                            t = "DRAWN HORIZONTALLY ORDERED TILE AS \"" + n + "\" (SPECIAL TILE RULES).";
                        else
                            t = "\"" + n + "\" DELAYED FOR HORIZONTAL ORDERING (SPECIAL TILE RULES).";
                    }
                    else
                    {
                        if (tat.d)
                            t = "DRAWN ORDERED TILE AS \"" + n + "\" (SPECIAL TILE RULES).";
                        else
                            t = "\"" + n + "\" DELAYED FOR ORDERING (SPECIAL TILE RULES).";
                    }
                }
                else
                {
                    if (tat.d)
                        t = "DRAWN AS \"" + n + "\" (SPECIAL TILE RULES).";
                    else
                        t = "SKIPPED.";
                }
            }
            else if (tat.o)
            {
                if (tat.h)
                {
                    if (tat.d)
                        t = "DRAWN HORIZONTALLY ORDERED TILE AS \"" + n + "\".";
                    else
                        t = "\"" + n + "\" DELAYED FOR HORIZONTAL ORDERING.";
                }
                else
                {
                    if (tat.d)
                        t = "DRAWN ORDERED TILE AS \"" + n + "\".";
                    else
                        t = "\"" + n + "\" DELAYED FOR ORDERING.";
                }
            }
            else
                t = "DRAWN AS \"" + n + "\"";

            return p + " [" + tile.tileCode + "] < y: " + LeftSpaceFiller(yLocation, yLocationMax.ToString().ToCharArray().Length, ' ') + " / x: " + LeftSpaceFiller(xLocation, xLocationMax.ToString().ToCharArray().Length, ' ') + " > " + t;
        }

        public enum ActionType { setup, tileDraw, orderedHorTileDraw, orderedTileDraw, saveLocation, aal, statusChange, basic, gamemodeModding }
        public class Voice
        {
            private Options savedOptionsObject;
            public TitleClass Title;
            public string version;

            public Voice()
            {
                savedOptionsObject = new Options { console = new Options.ConsoleOptions() { aal = true, orderedHorTileDraw = true, orderedTileDraw = true, saveLocation = true, setup = true, tileDraw = true }, saveLogFile = true };
                Title = new TitleClass();
            }

            public void UpdateOptions(Options optionsObject, string version)
            {
                savedOptionsObject = optionsObject;
                Title.UpdateObjects(optionsObject.title);
                Title.version = version;
            }

            List<string> loggedLines = new List<string>();

            public void Speak(string text, ActionType actionType) // Send a line to console + add to log
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
                            Console.WriteLine(" " + text);
                            loggedLines.Add(" " + text);
                        }
                        break;
                    case ActionType.statusChange:
                        if (savedOptionsObject.console.statusChange)
                        {
                            Console.WriteLine(text);
                            loggedLines.Add(text);
                        }
                        break;
                    case ActionType.gamemodeModding:
                        if (savedOptionsObject.console.gamemodeModding)
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

            public class TitleClass
            {
                public JobClass Job;
                public StatusClass Status;
                public string StatusDetails;
                public string version;
                public bool show;
                public Options.Title optionReference;

                public TitleClass()
                {
                    Job = new JobClass();
                    Status = new StatusClass();
                    StatusDetails = "";
                }

                public void UpdateObjects(Options.Title options)
                {
                    show = options != null;
                    if (!show)
                        return;
                    optionReference = options;
                    Job.titleObject = options.modules.job;
                    Status.titleObject = options.modules.status;
                }

                public class JobClass
                {
                    public int current;
                    public int max;
                    public string percentage;
                    public string progressBar;
                    public string jobsRatio;
                    public string jobName;
                    public Options.Job titleObject;

                    public JobClass UpdateJob(int currentJobIndex, int maxJobIndex, string job)
                    {
                        if (titleObject == null)
                            return this;
                        current = currentJobIndex;
                        max = maxJobIndex;
                        jobName = job;
                        jobsRatio = LeftSpaceFiller(currentJobIndex, maxJobIndex.ToString().Length, ' ') + "/" + maxJobIndex;
                        percentage = Math.Floor(Convert.ToDouble(currentJobIndex) / Convert.ToDouble(maxJobIndex) * 100) + "%";
                        progressBar = MakeProgressBar(10, titleObject.percentageBarFillCharacter, titleObject.percentageBarBackgroundCharacter, Convert.ToDouble(currentJobIndex) / Convert.ToDouble(maxJobIndex));

                        return this;
                    }

                    public JobClass UpdateJobName(string job)
                    {
                        jobName = job;

                        return this;
                    }

                    public JobClass IncreaseJob()
                    {
                        if (titleObject == null)
                            return this;
                        jobsRatio = LeftSpaceFiller(++current, max.ToString().Length, ' ') + "/" + max;
                        percentage = Math.Floor(Convert.ToDouble(current) / Convert.ToDouble(max) * 100) + "%";
                        progressBar = MakeProgressBar(10, titleObject.percentageBarFillCharacter, titleObject.percentageBarBackgroundCharacter, Convert.ToDouble(current) / Convert.ToDouble(max));

                        return this;
                    }
                }

                public string GetAppInfo()
                {
                    if (optionReference.modules.appInfo.showVersion)
                        return "BMG " + version;
                    else
                        return "BMG";
                }

                public enum StatusDetailsType { basic, biome, tile }

                public void UpdateStatusDetails(string newDetails, StatusDetailsType type)
                {
                    switch (type)
                    {
                        case StatusDetailsType.basic:
                            StatusDetails = newDetails;
                            break;
                        case StatusDetailsType.biome:
                            if (optionReference.modules.statusDetails.showBiome != false)
                                StatusDetails = newDetails;
                            break;
                        case StatusDetailsType.tile:
                            if (optionReference.modules.statusDetails.showTile != false)
                                StatusDetails = newDetails;
                            break;
                    }
                }

                public class StatusClass
                {
                    public int current;
                    public int max;
                    public string percentage;
                    public string progressBar;
                    public string actionRatio;
                    public string statusText;
                    public Options.Status titleObject;

                    public StatusClass UpdateStatus(int currentActionIndex, int maxActionIndex, string action)
                    {
                        if (titleObject == null)
                            return this;
                        current = currentActionIndex;
                        max = maxActionIndex;
                        statusText = action;
                        actionRatio = LeftSpaceFiller(currentActionIndex, maxActionIndex.ToString().Length, ' ') + "/" + maxActionIndex;
                        percentage = Math.Floor(Convert.ToDouble(currentActionIndex) / Convert.ToDouble(maxActionIndex) * 100) + "%";
                        progressBar = MakeProgressBar(10, titleObject.percentageBarFillCharacter, titleObject.percentageBarBackgroundCharacter, Convert.ToDouble(currentActionIndex) / Convert.ToDouble(maxActionIndex));

                        return this;
                    }

                    public StatusClass IncreaseStatus()
                    {
                        if (titleObject == null)
                            return this;
                        actionRatio = LeftSpaceFiller(++current, max.ToString().Length, ' ') + "/" + max;
                        percentage = Math.Floor(Convert.ToDouble(current) / Convert.ToDouble(max) * 100) + "%";
                        progressBar = MakeProgressBar(10, titleObject.percentageBarFillCharacter, titleObject.percentageBarBackgroundCharacter, Convert.ToDouble(current) / Convert.ToDouble(max));

                        return this;
                    }
                }

                public void RefreshTitle()
                {
                    if (!show)
                        return;
                    Console.Title = optionReference.layout
                        .Replace("?job?", Job.titleObject.order
                            .Replace("?percentage?", Job.percentage)
                            .Replace("?progressBar?", Job.progressBar)
                            .Replace("?jobName?", Job.jobName)
                            .Replace("?jobsRatio?", Job.jobsRatio))
                        .Replace("?status?", Status.titleObject.order
                            .Replace("?percentage?", Status.percentage)
                            .Replace("?progressBar?", Status.progressBar)
                            .Replace("?statusText?", Status.statusText)
                            .Replace("?actionRatio?", Status.actionRatio))
                        .Replace("?appInfo?", GetAppInfo())
                        .Replace("?statusDetails?", StatusDetails);
                }
            }

            public void Write(string fileName) // Save log file
            {
                if (savedOptionsObject.saveLogFile)
                {
                    if (savedOptionsObject.console.aal)
                        Console.WriteLine(" [ AAL ] WRITE >> " + "./" + fileName);
                    File.WriteAllText(fileName, string.Join("\n", loggedLines).Replace("\n", Environment.NewLine));
                    Console.WriteLine("\nLog saved!\n  Location: " + Path.GetFullPath("./" + fileName));
                }
                else
                    Console.WriteLine("\nLog saving is disabled.");
            }
        }

        public static string tileLinks(string[] map, int currentX, int currentY, Tiledata.Tile tileObject, Options.Replace[] replaces)
        {
            string binary = "";
            string neighbors;
            if (currentY == 0) // Top
            {
                if (currentX == 0)
                    neighbors = "%%%% %  "; // Left
                else if (currentX == map[0].Length - 1)
                    neighbors = "%%% %  %"; // Right
                else
                    neighbors = "%%%     "; // Middle
            }
            else if (currentY == map.Length - 1) // Bottom
            {
                if (currentX == 0)
                    neighbors = "%  % %%%"; // Left
                else if (currentX == map[0].Length - 1)
                    neighbors = "  % %%%%"; // Right
                else
                    neighbors = "     %%%"; // Middle
            }
            else // Middle
            {
                if (currentX == 0)
                    neighbors = "%  % %  "; // Left
                else if (currentX == map[0].Length - 1)
                    neighbors = "  % %  %"; // Right
                else
                    neighbors = "        "; // Middle
            }

            switch (tileObject.tileLinks.edgeCase)
            {
                case Tiledata.EdgeCase.different: // edges are filled with non-equal tiles
                    for (int x = 0; x < neighbors.Length; x++)
                    {
                        if (neighbors[x] == '%')
                            binary = binary + '0'; // edgeCase
                        else
                            binary = binary + checkNeighboringTile(map, currentX, currentY, tileObject, replaces, x); // check tile in map
                    }
                    break;
                case Tiledata.EdgeCase.copies: // edges are filled with equal tiles
                    for (int x = 0; x < neighbors.Length; x++)
                    {
                        if (neighbors[x] == '%')
                            binary = binary + '1'; // edgeCase
                        else
                            binary = binary + checkNeighboringTile(map, currentX, currentY, tileObject, replaces, x); // check tile in map
                    }
                    break;
                case Tiledata.EdgeCase.mirror: // edges are extended
                    for (int x = 0; x < neighbors.Length; x++)
                    {
                        if (neighbors[x] == '%')
                            if (x % 2 == 1)
                                binary = binary + '1'; // edgeCase (Edge adjacent edge tiles will always be equal when extending)
                            else
                            {
                                if (hasAdjacentEqualTiles(map, currentX - 1, currentY - 1, tileObject))
                                    binary = binary + '1';
                                else if (hasAdjacentEqualTiles(map, currentX - 1, currentY + 1, tileObject))
                                    binary = binary + '1';
                                else if (hasAdjacentEqualTiles(map, currentX + 1, currentY - 1, tileObject))
                                    binary = binary + '1';
                                else if (hasAdjacentEqualTiles(map, currentX + 1, currentY + 1, tileObject))
                                    binary = binary + '1';
                                else
                                    binary = binary + '0';
                            }
                        // binary = binary + checkNeighboringTile(map, currentX, currentY, tileObject, 7 - x); // edgeCase (check opposite tile to extend)
                        else
                            binary = binary + checkNeighboringTile(map, currentX, currentY, tileObject, replaces, x); // check tile in map
                    }
                    break;
            }

            return binary;
        }

        public static char checkNeighboringTile(string[] map, int currentX, int currentY, Tiledata.Tile tile, Options.Replace[] replaces, int neighbor)
        {
            switch (neighbor)
            {
                case 0:
                    if (CNTFilter(map[currentY - 1].ToCharArray()[currentX - 1], replaces) == tile.tileCode)
                        return '1';
                    else
                        return '0';
                case 1:
                    if (CNTFilter(map[currentY - 1].ToCharArray()[currentX], replaces) == tile.tileCode)
                        return '1';
                    else
                        return '0';
                case 2:
                    if (CNTFilter(map[currentY - 1].ToCharArray()[currentX + 1], replaces) == tile.tileCode)
                        return '1';
                    else
                        return '0';
                case 3:
                    if (CNTFilter(map[currentY].ToCharArray()[currentX - 1], replaces) == tile.tileCode)
                        return '1';
                    else
                        return '0';
                case 4:
                    if (CNTFilter(map[currentY].ToCharArray()[currentX + 1], replaces) == tile.tileCode)
                        return '1';
                    else
                        return '0';
                case 5:
                    if (CNTFilter(map[currentY + 1].ToCharArray()[currentX - 1], replaces) == tile.tileCode)
                        return '1';
                    else
                        return '0';
                case 6:
                    if (CNTFilter(map[currentY + 1].ToCharArray()[currentX], replaces) == tile.tileCode)
                        return '1';
                    else
                        return '0';
                case 7:
                    if (CNTFilter(map[currentY + 1].ToCharArray()[currentX + 1], replaces) == tile.tileCode)
                        return '1';
                    else
                        return '0';
                default:
                    return '0';
            }
        }

        private static char CNTFilter(char original, Options.Replace[] replaces)
        {
            foreach (var r in replaces)
                if (original == r.from)
                    return r.to;
            return original;
        }

        public static bool hasAdjacentEqualTiles(string[] map, int x, int y, Tiledata.Tile tileObject)
        {
            if (y < 0) // Top edge
            {
                if (x < 0) // Left corner
                {
                    if (map[y + 1].ToCharArray()[x + 1] == tileObject.tileCode)
                        return true;
                    else return false;
                }
                else if (x > map[0].Length - 1) // Right corner
                {
                    if (map[y + 1].ToCharArray()[x - 1] == tileObject.tileCode)
                        return true;
                    else return false;
                }
                else // Middle
                {
                    if (x != map[0].Length - 1)
                        if (map[y + 1].ToCharArray()[x + 1] == tileObject.tileCode)
                            return true;
                        else return false;
                    else if (x != 0)
                        if (map[y + 1].ToCharArray()[x - 1] == tileObject.tileCode)
                            return true;
                        else return false;
                    else return false;
                }
            }
            else if (y > map.Length - 1) // Bottom edge
            {
                if (x < 0) // Left corner
                {
                    if (map[y - 1].ToCharArray()[x + 1] == tileObject.tileCode)
                        return true;
                    else return false;
                }
                else if (x > map[0].Length - 1) // Right corner
                {
                    if (map[y - 1].ToCharArray()[x - 1] == tileObject.tileCode)
                        return true;
                    else return false;
                }
                else // Middle
                {
                    if (x != map[0].Length - 1)
                        if (map[y - 1].ToCharArray()[x + 1] == tileObject.tileCode)
                            return true;
                        else return false;
                    else if (x != 0)
                        if (map[y - 1].ToCharArray()[x - 1] == tileObject.tileCode)
                            return true;
                        else return false;
                    else return false;
                }
            }
            else // -
            {
                if (x < 0) // Left edge
                {
                    if (y != 0)
                        if (map[y - 1].ToCharArray()[x + 1] == tileObject.tileCode)
                            return true;
                        else return false;
                    else if (y != map.Length - 1)
                        if (map[y + 1].ToCharArray()[x + 1] == tileObject.tileCode)
                            return true;
                        else return false;
                    else return false;
                }
                else if (x > map[0].Length - 1) // Right edge
                {
                    if (y != 0)
                        if (map[y - 1].ToCharArray()[x - 1] == tileObject.tileCode)
                            return true;
                        else return false;
                    else if (y != map.Length - 1)
                        if (map[y + 1].ToCharArray()[x - 1] == tileObject.tileCode)
                            return true;
                        else return false;
                    else return false;
                }
                else // -
                {
                    return false;
                }
            }

        }

        static float[] emptyBorderAmoutNormalizer(float[] raw)
        {
            float[] normalized = new float[4];

            switch (raw.Length)
            {
                case 0:
                    normalized[0] = 1;
                    normalized[1] = 1;
                    normalized[2] = 1;
                    normalized[3] = 1;
                    break;
                case 1:
                    normalized[0] = raw[0];
                    normalized[1] = raw[0];
                    normalized[2] = raw[0];
                    normalized[3] = raw[0];
                    break;
                case 2:
                    normalized[0] = raw[0];
                    normalized[1] = raw[0];
                    normalized[2] = raw[1];
                    normalized[3] = raw[1];
                    break;
                case 3:
                    normalized[0] = raw[0];
                    normalized[1] = raw[1];
                    normalized[2] = raw[2];
                    normalized[3] = raw[2];
                    break;
                case 4:
                    normalized = raw;
                    break;
            }

            return normalized;
        }

    }

}
