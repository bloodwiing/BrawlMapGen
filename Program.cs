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
            int minor = 9;
            int patch = 1;
            string access = "Release";
            string oLoc = "options.json";
            string oStr = "";
            string oEnd = "";

            // Fun statistical ending numbers
            int tilesDrawn = 0;
            int mapsDrawn = 0;
            List<char> tilesFailedChars = new List<char>();
            Dictionary<char, int> tilesFailed = new Dictionary<char, int>();

            Options1 options = new Options1();
            Logger logger = new Logger();

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
                        logger.LogError("Option file doesn't exist\n  [FileReader] Unable to find file in location \"" + oLoc + "\"");
                        logger.Save("log.txt");
                        Thread.Sleep(3000);
                        Environment.Exit(1);
                    }

                    StreamReader r = new StreamReader(oLoc);
                    oStr = r.ReadToEnd();
                }

                var format = JsonConvert.DeserializeObject<OptionsBase>(oStr);
                if (format.format == 1)
                    options = JsonConvert.DeserializeObject<Options1>(oStr);
                else
                    throw new ArgumentException("Invalid OPTIONS format");

                logger.UpdateOptions(options, major + "." + minor + "." + patch);

                if (options.setPath != null)
                    Environment.CurrentDirectory = options.setPath;

                logger.LogSpacer();
                
                logger.Log("  BMG (Brawl Map Gen)");
                logger.Log(string.Format("    Version: v{0}.{1}.{2} {3}", major, major, patch, access));
                logger.Log("    Created by: RedH1ghway (aka BloodWiing)");
                logger.Log("    Helped by: 4JR, Henry, tryso");

                logger.LogSpacer();
                logger.LogSpacer();

                logger.LogStatus("App is launched!");
                logger.LogSetup("Loading preset: \"" + options.preset.ToUpper() + "\"...");
                logger.LogAAL(Logger.AALDirection.In, "./presets/" + options.preset + ".json");

                logger.Title.Job.UpdateJob(0, 1, "Preparing...");
                logger.Title.Status.UpdateStatus(0, 1, "Loading preset...");
                logger.Title.UpdateStatusDetails(options.preset.ToUpper(), Logger.TitleClass.StatusDetailsType.basic);
                logger.Title.RefreshTitle();

                if (!File.Exists("./presets/" + options.preset + ".json"))
                {
                    logger.LogError("Preset doesn't exist\n  [FileReader] Unable to find file in location \"presets/" + options.preset + ".json\"");
                    logger.Save("log.txt");
                    Thread.Sleep(3000);
                    Environment.Exit(1);
                }

                StreamReader r2 = new StreamReader("./presets/" + options.preset + ".json");
                string json2 = r2.ReadToEnd();
                logger.LogSetup("Preset loaded: \"" + options.preset.ToUpper() + "\"!", false);
                logger.LogStatus("Tiles will be drawn from preset \"" + options.preset.ToUpper() + "\".");
                var tiledata = JsonConvert.DeserializeObject<Tiledata>(json2);
                Dictionary<int, SavedImages> savedTileImageList = new Dictionary<int, SavedImages>();

                int totalSizes, totalImages = 0;

                {  // Local calculations
                    List<int> sizes = new List<int>();
                    foreach (Options1.BatchSettings single in options.batch)
                        if (!sizes.Contains(single.sizeMultiplier))
                            sizes.Add(single.sizeMultiplier);
                    totalSizes = sizes.Count;
                }

                foreach (string folder in Directory.GetDirectories("./assets/tiles/" + options.preset + "/"))
                    totalImages += Directory.GetFiles(folder).Length;
                totalImages += Directory.GetFiles("./assets/tiles/" + options.preset + "/").Length;

                logger.LogSpacer();

                logger.Title.Job.UpdateJob(0, totalSizes, "Gathering render data...");
                logger.Title.RefreshTitle();
                logger.LogStatus("Checking inclusions and exclusions.");

                {
                    List<Options1.BatchSettings> final = new List<Options1.BatchSettings>();
                    for (int i = 0; i < options.batch.Length; i++)
                        if ((options.render.include.Length == 0 || options.render.include.Contains(i)) // Include check
                            && (options.render.exclude.Length == 0 || !options.render.exclude.Contains(i))) // Exclude check
                            final.Add(options.batch[i]);
                    options.batch = final.ToArray();
                }
                logger.LogStatus("Selective render ready.");
                if (options.render.include.Length > 0)
                    logger.LogSetup("Inclusions: " + string.Join(", ", options.render.include) + ".", false);
                if (options.render.exclude.Length > 0)
                    logger.LogSetup("Exclusions: " + string.Join(", ", options.render.exclude) + ".", false);

                logger.LogSpacer();

                if (options.batch.Length == 0)
                    throw new ArgumentException("Missing batch data");

                if (options.autoCrop.enabled && options.autoCrop.tiles.Length == 0)
                {
                    options.autoCrop.tiles = options.batch[0].skipTiles;
                    logger.LogWarning("AutoCrop is enabled, but empty. Defaulting to OPTIONS' first map's skipTiles.\n Please make sure to update this setting", 15);
                    Thread.Sleep(10000);
                }

                logger.Title.Job.UpdateJob(0, totalSizes, "Preloading tiles...");
                logger.Title.RefreshTitle();
                logger.LogStatus("Tile Preloading started.");
                foreach (Options1.BatchSettings single in options.batch) // Tile preloader
                {
                    if (savedTileImageList.ContainsKey(single.sizeMultiplier))
                        continue;

                    logger.Title.Job.IncreaseJob();
                    logger.Title.Status.UpdateStatus(0, totalImages, "Reading...");
                    logger.Title.RefreshTitle();

                    logger.LogSpacer();
                    logger.LogSetup(string.Format("Found new tilesize. ({0})", single.sizeMultiplier), false);
                    savedTileImageList.Add(single.sizeMultiplier, new SavedImages(options, tiledata.tiles, single.sizeMultiplier, logger)); // Preload tiles for a specific tiles
                }
                logger.LogSpacer();
                logger.LogStatus("Tile Preload complete.");
                logger.LogSetup("Preloaded tiles with tilesizes:", false);
                foreach (var si in savedTileImageList)
                    logger.LogSetup("  " + si.Key + "px", false);

                int bNumber = -1;
                logger.LogSpacer();
                logger.LogStatus("Map image generator starting...");
                logger.Title.Job.UpdateJob(0, options.batch.Length, "\"temp_name\"");
                foreach (var batchOption in options.batch)
                {
                    logger.Title.Job.UpdateJobName(("\"" + batchOption.name + "\"").Replace("\"?number?\"", "Number " + bNumber));
                    logger.Title.Job.IncreaseJob();

                    SavedImages selectedTileImageList = savedTileImageList[batchOption.sizeMultiplier];

                    bNumber++;

                    logger.LogSpacer();
                    logger.LogStatus("Getting map...");
                    logger.LogSetup("Looking for map number " + bNumber + "...");

                    var map = batchOption.map;
                    var sizeMultiplier = batchOption.sizeMultiplier;

                    if (map == null)
                    {
                        logger.LogWarning("  Map is empty!\n  [Object] Map in the index number " + bNumber + " is not defined.", 4);
                        continue;
                    }

                    logger.LogSetup("  Map found.", false);
                    logger.LogStatus("Map gotten.");

                    if (options.autoCrop.enabled)
                    {
                        int t, b, l = map[0].Length, r = 0;
                        string line;

                        for (t = 0; t < map.Length; t++)
                        {
                            line = map[t];
                            foreach (char c in options.autoCrop.tiles)
                                line = line.Replace(c.ToString(), string.Empty);
                            if (line != string.Empty)
                                break;
                        }

                        for (b = map.Length - 1; b >= 0; b--)
                        {
                            line = map[b];
                            foreach (char c in options.autoCrop.tiles)
                                line = line.Replace(c.ToString(), string.Empty);
                            if (line != string.Empty)
                                break;
                        }

                        for (int e = t; e <= b; e++)
                        {
                            line = map[e];
                            if (line.Length - line.TrimStart(options.autoCrop.tiles).Length < l)
                                l = line.Length - line.TrimStart(options.autoCrop.tiles).Length;
                            if (line.Length - line.TrimEnd(options.autoCrop.tiles).Length < r)
                                r = line.Length - line.TrimEnd(options.autoCrop.tiles).Length;
                        }

                        if (t != 0 || b != map.Length - 1 || l != 0 || r != 0)
                        {
                            map = map
                                .Skip(t)
                                .Take(b - t + 1)
                                .Select(item => item.Substring(l, item.Length - l - r))
                                .ToArray();

                            logger.LogSpacer();
                            logger.LogSetup("Auto-Cropped map:", false);
                            logger.LogSetup(string.Format(
                                "  {0} Top\n  {1} Bottom\n  {2} Left\n  {3} Right",
                                t, batchOption.map.Length - b - 1, l, r
                            ), false);
                        }
                    }

                    if (map.Length == 0 || map[0].Length == 0)
                    {
                        logger.LogWarning("Map is empty!\n  [Object] Map in the index number " + bNumber + " has no string arrays.", 4);
                        continue;
                    }

                    // Preparing and drawing background
                    int xLength = map[0].Length;
                    int yLength = map.Length;

                    Tiledata.Biome mapBiome = tiledata.GetBiome(batchOption.biome);
                    logger.LogSpacer();
                    logger.Log("Map details:\n  Width: " + (sizeMultiplier * 2 + sizeMultiplier * xLength) + "px\n  Height: " + (sizeMultiplier * 2 + sizeMultiplier * yLength) + "px\n  Biome: \"" + mapBiome.name.ToUpper() + "\"\n");

                    float[] border = emptyBorderAmoutNormalizer(batchOption.emptyBorderAmount);

                    TileDrawer tileDrawer = new TileDrawer(batchOption.sizeMultiplier, map[0].Length, map.Length, border);

                    logger.LogSetup("Coloring background...");
                    logger.LogStatus("Fetching tile colors...");

                    string[] color1s = mapBiome.color1.Split(',');
                    Color color1 = Color.FromArgb(int.Parse(color1s[0].Trim()), int.Parse(color1s[1].Trim()), int.Parse(color1s[2].Trim()));

                    string[] color2s = mapBiome.color2.Split(',');
                    Color color2 = Color.FromArgb(int.Parse(color2s[0].Trim()), int.Parse(color2s[1].Trim()), int.Parse(color2s[2].Trim()));

                    logger.LogStatus("Colors fetched.");
                    logger.LogStatus("Coloring background...");

                    tileDrawer.ColorBackground(color1, color2, map, batchOption, border);
                    logger.LogStatus("Background colored.");

                    logger.LogSetup("Drawing map tiles...");
                    if (batchOption.name != "?number?")
                        logger.LogStatus("Drawing map (\"" + batchOption.name + "\")...");
                    else
                        logger.LogStatus("Drawing map (#" + bNumber + ")...");

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
                                            logger.LogTile(new TileActionTypes(1, 0, 0, 0, 1), oTile, ysLoc, xsLoc, yLength, xLength, Logger.TileEvent.tileDraw);
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

                                        logger.LogTile(new TileActionTypes(1, 0, 1, 0, 0), oTile, ysLoc, xsLoc, yLength, xLength, Logger.TileEvent.gamemodeModding);
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

                        logger.Title.Status.UpdateStatus(0, map.Length * map[0].Length, string.Format("Drawing tiles... PASS {0}", drawPass));
                        // Begin to draw map
                        foreach (string row in map)
                        {
                            List<OrderedTile> orderedHorTiles = new List<OrderedTile>();

                            foreach (char tTile in row.ToCharArray())
                            {
                                logger.Title.Status.IncreaseStatus();
                                logger.Title.RefreshTitle();

                                bool tileDrawn = false;

                                var tile = tTile;

                                foreach (Options1.Replace repTile in batchOption.replaceTiles) // Specified Tile Code Replacer
                                {
                                    if (tile == repTile.from)
                                        tile = repTile.to;
                                }

                                if (batchOption.skipTiles.Contains(tile)) // Specified Tile Skipper
                                {
                                    logger.LogTile(new TileActionTypes(0, 1, 0, 0, 0), new Tiledata.Tile() { tileName = "", tileCode = tile }, currentY, currentX, yLength, xLength, Logger.TileEvent.tileDraw);
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
                                                                        logger.LogTile(new TileActionTypes(0, 1, 1, 0, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                                                        orderedTiles.Add(new OrderedTile()
                                                                        {
                                                                            tileType = aTile.tileTypes[ostr.tileType],
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
                                                                        logger.LogTile(new TileActionTypes(0, 1, 1, 1, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedHorTileDraw);
                                                                        orderedHorTiles.Add(new OrderedTile()
                                                                        {
                                                                            tileType = aTile.tileTypes[ostr.tileType],
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
                                                                    tileDrawer.DrawTile(aTile, ostr.tileType, options, sizeMultiplier, currentX, currentY, xLength, yLength, selectedTileImageList, border);
                                                                    tilesDrawn++;
                                                                    logger.LogTile(new TileActionTypes(0, 1, 0, 0, 1), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.tileDraw);
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
                                                    var defaultType = aTile.tileTypes[aTile.tileLinks.defaults.tileType];
                                                    foreach (Tiledata.TileLinkRule aRule in accurateRules)
                                                    {
                                                        if (aRule.changeBinary != null)
                                                            for (int y = 0; y < aRule.changeBinary.Length; y++)
                                                            {
                                                                nbca[int.Parse(aRule.changeBinary[y].Split('a')[1])] = aRule.changeBinary[y].Split('a')[0].ToCharArray()[0];
                                                            }
                                                        if (aRule.changeTileType != null)
                                                            defaultType = aTile.tileTypes[aRule.changeTileType.GetValueOrDefault()];
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
                                                            logger.LogTile(new TileActionTypes(0, 0, 1, 0, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                                            orderedTiles.Add(new OrderedTile()
                                                            {
                                                                tileType = breakerTile,
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
                                                            logger.LogTile(new TileActionTypes(0, 0, 1, 1, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedHorTileDraw);
                                                            orderedHorTiles.Add(new OrderedTile()
                                                            {
                                                                tileType = breakerTile,
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
                                                        tileDrawer.DrawSelectedTile(new OrderedTile() { tileType = breakerTile, xPosition = currentX, yPosition = currentY, tileCode = aTile.tileCode, tileName = aTile.tileName }, options, sizeMultiplier, xLength, yLength, selectedTileImageList, border);
                                                        logger.LogTile(new TileActionTypes(0, 0, 0, 0, 1), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.tileDraw);
                                                        tilesDrawn++;
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
                                                        logger.LogTile(new TileActionTypes(0, 0, 1, 0, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                                        orderedTiles.Add(new OrderedTile()
                                                        {
                                                            tileType = aTile.tileTypes[setTileDefault.type],
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
                                                        logger.LogTile(new TileActionTypes(0, 0, 1, 1, 0), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.orderedHorTileDraw);
                                                        orderedHorTiles.Add(new OrderedTile()
                                                        {
                                                            tileType = aTile.tileTypes[setTileDefault.type],
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
                                                    tileDrawer.DrawTile(aTile, setTileDefault.type, options, sizeMultiplier, currentX, currentY, xLength, yLength, selectedTileImageList, border);
                                                    tilesDrawn++;
                                                    logger.LogTile(new TileActionTypes(0, 0, 0, 0, 1), aTile, currentY, currentX, yLength, xLength, Logger.TileEvent.tileDraw);
                                                }
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
                            int lowestHorOrder = 0, highestHorOrder = 0;
                            foreach (var pTile in orderedHorTiles)
                            {
                                if (pTile == null)
                                    continue;

                                var value = pTile.tileType.orderHor.GetValueOrDefault();

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
                                    if (pTile.tileType.orderHor.GetValueOrDefault() != currentHorOrdered)
                                        continue;

                                    tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, xLength, yLength, selectedTileImageList, border);
                                    if (pTile.str)
                                        logger.LogTile(new TileActionTypes(0, 1, 1, 1, 1), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength, Logger.TileEvent.orderedHorTileDraw);
                                    else
                                        logger.LogTile(new TileActionTypes(0, 0, 1, 1, 1), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength, Logger.TileEvent.orderedHorTileDraw);
                                    tilesDrawn++;
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

                            var value = pTile.tileType.order.GetValueOrDefault();

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
                                if (pTile.tileType.order.GetValueOrDefault() != currentOrdered)
                                    continue;

                                tileDrawer.DrawSelectedTile(pTile, options, sizeMultiplier, xLength, yLength, selectedTileImageList, border);
                                if (pTile.str)
                                    logger.LogTile(new TileActionTypes(0, 1, 1, 0, 1), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                else
                                    logger.LogTile(new TileActionTypes(0, 0, 1, 0, 1), new Tiledata.Tile() { tileCode = pTile.tileCode, tileName = pTile.tileName }, pTile.yPosition, pTile.xPosition, yLength, xLength, Logger.TileEvent.orderedTileDraw);
                                tilesDrawn++;
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

                                                tileDrawer.DrawTile(oTile, st.type, options, sizeMultiplier, xLoc, yLoc, xLength, yLength, selectedTileImageList, border);
                                                tilesDrawn++;
                                                logger.LogTile(new TileActionTypes(1, 0, 0, 0, 1), oTile, ysLoc, xsLoc, yLength, xLength, Logger.TileEvent.tileDraw);
                                            }
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
                    logger.LogStatus("Map drawn.");
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
                                logger.LogAAL(Logger.AALDirection.Out, options.exportFolderName + "/" + exportName);
                                logger.LogExport(exportName);
                            }
                            else
                            {
                                tileDrawer.ExportImage(options, exportName);
                                logger.LogAAL(Logger.AALDirection.Out, options.exportFolderName + "/" + exportName);
                                logger.LogExport(exportName);
                            }
                        }
                        else
                        {
                            if (batchOption.exportFileName != null)
                                tileDrawer.ExportImage(options, batchOption.exportFileName);
                            else
                                tileDrawer.ExportImage(options, exportName);
                            logger.LogAAL(Logger.AALDirection.Out, exportName);
                            logger.LogExport(exportName);
                        }
                    }
                    else
                    {
                        if (batchOption.exportFileName != null)
                            tileDrawer.ExportImage(options, batchOption.exportFileName);
                        else
                            tileDrawer.ExportImage(options, exportName);
                        logger.LogAAL(Logger.AALDirection.Out, exportName);
                        logger.LogExport(exportName);
                    }

                    mapsDrawn++;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e);
            }

            stopwatch.Stop();
            logger.LogSpacer();
            logger.Log("Finished.");

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

            logger.LogSpacer();
            logger.Log("Results:\n  Total Maps Drawn: " + mapsDrawn + "\n  Total Tiles Drawn: " + tilesDrawn + "\n  Completed in: " + stTime);

            if (tilesFailed.Count == 0)
                logger.Log("  No unrecognized tiles encountered.");
            else
            {
                logger.Log("  Unrecognized tiles encountered:");
                foreach (var t in tilesFailedChars)
                    logger.Log("    \"" + t + "\": " + tilesFailed[t]);
            }

            logger.LogSpacer();

            logger.Save("log.txt");

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

            public TileActionTypes(byte _g, byte _s, byte _o, byte _h, byte _d)
            {
                g = _g == 1;
                s = _s == 1;
                o = _o == 1;
                h = _h == 1;
                d = _d == 1;
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

            public void DrawTile(Tiledata.Tile tile, int type, Options1 optionsObject, int sizeMultiplier, int currentX, int currentY, int xLength, int yLength, SavedImages imageMemory, float[] borderSize) // Drawing a tile (normal)
            {
                foreach (SavedImages.TileImage ti in imageMemory.tileImages)
                {
                    if (ti.imageName == tile.tileTypes[type].asset)
                    {
                        g.DrawImage(ti.renderedImage, (int)Math.Round(sizeMultiplier * (currentX + borderSize[2])) - ti.imageOffsetLeft, (int)Math.Round(sizeMultiplier * (currentY + borderSize[0])) - ti.imageOffsetTop);
                        return;
                    }
                }
            }

            public void DrawSelectedTile(OrderedTile tile, Options1 optionsObject, int sizeMultiplier, int xLength, int yLength, SavedImages imageMemory, float[] borderSize) // Drawing a tile (with saved coordinates and a pre-selected type)
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

            public void ColorBackground(Color color1, Color color2, string[] map, Options1.BatchSettings batchOption, float[] borderSize) // Filling in background colors
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

            public void ExportImage(Options1 optionsObject, string fileName) // Saving the generated image
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
            return TileActionStringMaker(tat, tile, yLocation.ToString(), xLocation.ToString(), yLocationMax, xLocationMax);
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

        public class Logger
        {
            private Options1 savedOptionsObject;
            public TitleClass Title;
            public string version;

            public Logger()
            {
                savedOptionsObject = new Options1 { console = new Options1.ConsoleOptions() { aal = true, orderedHorTileDraw = true, orderedTileDraw = true, saveLocation = true, setup = true, tileDraw = true }, saveLogFile = true };
                Title = new TitleClass();
            }

            public void UpdateOptions(Options1 optionsObject, string version)
            {
                savedOptionsObject = optionsObject;
                Title.UpdateObjects(optionsObject.title);
                Title.version = version;
                Console.Title = Title.GetAppInfo();
            }

            List<string> loggedLines = new List<string>();

            public void Log(string text) // Send a line to console + add to log
            {
                Console.WriteLine(text);
                loggedLines.Add(text);
            }

            public void LogSpacer() // Empty line
            {
                Console.WriteLine();
                loggedLines.Add("");
            }

            public enum AALDirection { In, Out }
            public void LogAAL(AALDirection direction, string file) // Log AAL events
            {
                if (!savedOptionsObject.console.aal) return;
                if (direction == AALDirection.In)
                    Log(" [ AAL ] READ << " + file);
                else
                    Log(" [ AAL ] WRITE >> " + file);
            }

            public void LogStatus(string text) // Log status changes
            {
                if (!savedOptionsObject.console.statusChange) return;
                Log(" Status: " + text);
            }

            public void LogSetup(string text, bool prefix = true) // Log setup jobs
            {
                if (!savedOptionsObject.console.setup) return;
                if (prefix)
                    Log("New job: " + text);
                else
                    Log(text);
            }

            public void LogExport(string file) // Log setup jobs
            {
                if (!savedOptionsObject.console.saveLocation) return;
                LogSpacer();
                if (Regex.IsMatch(file, "\\S:"))
                    Log("Image saved!\n  Location: " + file);
                else
                    Log("Image saved!\n  Location: " + Path.GetFullPath("./" + file));
            }

            public enum TileEvent { tileDraw, orderedHorTileDraw, orderedTileDraw, gamemodeModding }

            public void LogTile(TileActionTypes tat, Tiledata.Tile tile, int y, int x, int yMax, int xMax, TileEvent tileEvent) // Log tile events
            {
                LogTile(tat, tile, y.ToString(), x.ToString(), yMax, xMax, tileEvent);
            }

            public void LogTile(TileActionTypes tat, Tiledata.Tile tile, string y, string x, int yMax, int xMax, TileEvent tileEvent) // Log tile events
            {
                if (tileEvent == TileEvent.tileDraw && !savedOptionsObject.console.tileDraw) return;
                if (tileEvent == TileEvent.orderedHorTileDraw && !savedOptionsObject.console.orderedHorTileDraw) return;
                if (tileEvent == TileEvent.orderedTileDraw && !savedOptionsObject.console.orderedTileDraw) return;
                if (tileEvent == TileEvent.gamemodeModding && !savedOptionsObject.console.gamemodeModding) return;

                Log(TileActionStringMaker(tat, tile, y, x, yMax, xMax));
            }

            public void LogWarning(string text, int timeout = 10) // Log a warning and pause
            {
                LogSpacer();
                Log(" WARNING: " + text);
                Log(string.Format(" Resuming in {0} seconds...", timeout));
                Thread.Sleep(timeout * 1000);
            }

            public void LogError(Exception error) // Log an error
            {
                LogError(error.ToString());
            }

            public void LogError(string error) // Log an error
            {
                LogSpacer();
                Log(" !! FATAL ERROR:\n" + error);
            }

            public class TitleClass
            {
                public JobClass Job;
                public StatusClass Status;
                public string StatusDetails;
                public string version;
                public bool show;
                public Options1.Title optionReference;

                public TitleClass()
                {
                    Job = new JobClass();
                    Status = new StatusClass();
                    StatusDetails = "";
                }

                public void UpdateObjects(Options1.Title options)
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
                    public Options1.Job titleObject;

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
                    public Options1.Status titleObject;

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
                    if (!show || optionReference.disableUpdate)
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

            public void Save(string fileName) // Save log file
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

        public static string tileLinks(string[] map, int currentX, int currentY, Tiledata.Tile tileObject, Options1.Replace[] replaces)
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

        public static char checkNeighboringTile(string[] map, int currentX, int currentY, Tiledata.Tile tile, Options1.Replace[] replaces, int neighbor)
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

        private static char CNTFilter(char original, Options1.Replace[] replaces)
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
