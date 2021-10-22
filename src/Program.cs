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

            string oLoc = "options.json";
            string oStr = "";
            string oEnd = "";

            Dictionary<char, int> tilesFailed = new Dictionary<char, int>();

            Options1 options;

            try
            {
                AMGState.StartTimer();

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

                var format = JsonConvert.DeserializeObject<OptionsBase>(oStr);
                if (format.format == 1)
                    options = JsonConvert.DeserializeObject<Options1>(oStr);
                else
                    throw new ArgumentException("Invalid OPTIONS format");

                Logger.UpdateOptions(options, AMGState.version.major + "." + AMGState.version.minor + "." + AMGState.version.patch);

                if (options.setPath != null)
                    Environment.CurrentDirectory = options.setPath;

                Logger.LogSpacer();
                
                Logger.Log("  BMG (Brawl Map Gen)");
                Logger.Log("    Created by: BloodWiing");
                Logger.Log("    Helped by: 4JR, Henry, tryso");

                Logger.LogSpacer();

                Logger.Log("  VERSION");
                Logger.Log(string.Format("    BrawlMapGen -- {0}", AMGState.version));
                Logger.Log(string.Format("    AMG!Blocks -- {0}", AMGBlocks.version));

                Logger.LogSpacer();
                Logger.LogSpacer();

                Logger.LogStatus("App is launched!");
                Logger.LogSetup("Loading preset: \"" + options.preset.ToUpper() + "\"...");

                Logger.Title.Job.UpdateJob(0, 1, "Preparing...");
                Logger.Title.Status.UpdateStatus(0, 1, "Loading preset...");
                Logger.Title.UpdateStatusDetails(options.preset.ToUpper(), Logger.TitleClass.StatusDetailsType.basic);
                Logger.Title.RefreshTitle();

                if (!File.Exists("./presets/" + options.preset + ".json"))
                {
                    Logger.LogError("Preset doesn't exist\n  [FileReader] Unable to find file in location \"presets/" + options.preset + ".json\"");
                    Logger.Save("log.txt");
                    Thread.Sleep(3000);
                    Environment.Exit(1);
                }

                Logger.LogAAL(Logger.AALDirection.In, "./presets/" + options.preset + ".json");
                StreamReader r2 = new StreamReader("./presets/" + options.preset + ".json");
                string json2 = r2.ReadToEnd();
                r2.Close();
                Logger.LogSetup("Preset loaded: \"" + options.preset.ToUpper() + "\"!", false);
                Logger.LogStatus("Tiles will be drawn from preset \"" + options.preset.ToUpper() + "\".");
                var tiledata = JsonConvert.DeserializeObject<Tiledata>(json2, new AMGBlockReader());
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

                Logger.LogSpacer();

                Logger.Title.Job.UpdateJob(0, totalSizes, "Gathering render data...");
                Logger.Title.RefreshTitle();
                Logger.LogStatus("Checking inclusions and exclusions.");

                {
                    List<Options1.BatchSettings> final = new List<Options1.BatchSettings>();
                    for (int i = 0; i < options.batch.Length; i++)
                        if ((options.render.include.Length == 0 || options.render.include.Contains(i)) // Include check
                            && (options.render.exclude.Length == 0 || !options.render.exclude.Contains(i))) // Exclude check
                            final.Add(options.batch[i]);
                    options.batch = final.ToArray();
                }
                Logger.LogStatus("Selective render ready.");
                if (options.render.include.Length > 0)
                    Logger.LogSetup("Inclusions: " + string.Join(", ", options.render.include) + ".", false);
                if (options.render.exclude.Length > 0)
                    Logger.LogSetup("Exclusions: " + string.Join(", ", options.render.exclude) + ".", false);

                Logger.LogSpacer();

                if (options.batch.Length == 0)
                    throw new ArgumentException("Missing batch data");

                if (options.autoCrop.enabled && options.autoCrop.tiles.Length == 0)
                {
                    options.autoCrop.tiles = options.batch[0].skipTiles;
                    Logger.LogWarning("AutoCrop is enabled, but empty. Defaulting to OPTIONS' first map's skipTiles.\n Please make sure to update this setting", 15);
                    Thread.Sleep(10000);
                }

                foreach (Options1.BatchSettings single in options.batch) // Tile size register
                {
                    if (savedTileImageList.ContainsKey(single.sizeMultiplier))
                        continue;

                    var si = new SavedImages(options, single.sizeMultiplier);

                    if (options.randomizers.enabled && options.randomizers.seed != null)
                        si.SetRandomSeed(options.randomizers.seed.GetValueOrDefault());

                    savedTileImageList.Add(single.sizeMultiplier, si); // Register tile size
                }
                Logger.LogSetup("Registered tilesizes:", false);
                foreach (var si in savedTileImageList)
                    Logger.LogSetup("  " + si.Key + "px", false);
                Logger.LogSpacer();

                Logger.LogStatus("Map image generator starting...");
                Logger.Title.Job.UpdateJob(0, options.batch.Length, "\"temp_name\"");
                foreach (var batchOption in options.batch)
                {
                    Logger.Title.Job.UpdateJobName(("\"" + batchOption.name + "\"").Replace("\"?number?\"", "Number " + AMGState.map.index));
                    Logger.Title.Job.IncreaseJob();

                    SavedImages selectedTileImageList = savedTileImageList[batchOption.sizeMultiplier];

                    if (batchOption.randomSeed != null)
                        selectedTileImageList.SetRandomSeed(batchOption.randomSeed.GetValueOrDefault());

                    Logger.LogSpacer();
                    Logger.LogStatus("Getting map...");
                    Logger.LogSetup("Looking for map number " + AMGState.map.index + "...");

                    var sizeMultiplier = batchOption.sizeMultiplier;

                    if (batchOption.map == null)
                    {
                        Logger.LogWarning("  Map is empty!\n  [Object] Map in the index number " + AMGState.map.index + " is not defined.", 4);
                        continue;
                    }

                    Logger.LogSetup("  Map found.", false);
                    Logger.LogStatus("Map gotten.");

                    AMGState.NewMap(batchOption);
                    if (options.autoCrop.enabled)
                        AMGState.map.AutoCrop(options.autoCrop.tiles);

                    if (!AMGState.map.valid)
                        continue;

                    // Preparing and drawing background
                    int xLength = AMGState.map.size.width;
                    int yLength = AMGState.map.size.height;

                    Tiledata.Biome mapBiome = tiledata.GetBiome(batchOption.biome);
                    Logger.LogSpacer();
                    Logger.Log("Map details:\n  Width: " + (sizeMultiplier * 2 + sizeMultiplier * xLength) + "px\n  Height: " + (sizeMultiplier * 2 + sizeMultiplier * yLength) + "px\n  Biome: \"" + mapBiome.name.ToUpper() + "\"\n");

                    float[] border = emptyBorderAmoutNormalizer(batchOption.emptyBorderAmount);

                    TileDrawer tileDrawer = new TileDrawer(batchOption.sizeMultiplier, xLength, yLength, border, tiledata);

                    Logger.LogSetup("Drawing background...");
                    Logger.LogStatus("Running AMG!Blocks...");

                    tileDrawer.ColorBackground(tiledata, batchOption, border);

                    Logger.LogStatus("Background drawn.");

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

        public static float[] emptyBorderAmoutNormalizer(float[] raw)
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

        public TileActionTypes(bool _m, bool _g, bool _s, bool _o, bool _h, bool _d)
        {
            m = _m;
            g = _g;
            s = _s;
            o = _o;
            h = _h;
            d = _d;
        }

        public TileActionTypes(byte _m, byte _g, byte _s, byte _o, byte _h, byte _d)
        {
            m = _m == 1;
            g = _g == 1;
            s = _s == 1;
            o = _o == 1;
            h = _h == 1;
            d = _d == 1;
        }

        public bool m = false;
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
        Tiledata t;

        public TileDrawer(int sizeMultiplier, int horizontalLengthInTiles, int verticalLengthInTiles, float[] borderSize, Tiledata tiledata)
        {
            b = new Bitmap((int)Math.Round(sizeMultiplier * (borderSize[2] + borderSize[3] + horizontalLengthInTiles)), (int)Math.Round(sizeMultiplier * (borderSize[0] + borderSize[1] + verticalLengthInTiles)));
            g = Graphics.FromImage(b);
            t = tiledata;
        }

        private Tiledata.TileType GetRealAsset(Tiledata.Tile tile, int type, Options1 optionsObject, string defaultAsset, int? overrideType = null)
        {
            Tiledata.TileType asset = tile.tileTypes[overrideType.GetValueOrDefault(type)];
            asset.asset = defaultAsset;

            if (optionsObject.assetSwitchers != null)
                foreach (Options1.AssetSwitcher switcher in optionsObject.assetSwitchers)
                {
                    if (tile.tileName == switcher.find.tile && type == switcher.find.type)
                        foreach (Tiledata.Tile dTile in t.tiles)
                            if (dTile.tileName == switcher.replace.tile)
                                asset = dTile.tileTypes[switcher.replace.type];

                }

            return asset;
        }

        private Tiledata.TileType GetRealAsset(OrderedTile tile, int type, Options1 optionsObject, string defaultAsset)
        {
            return GetRealAsset(new Tiledata.Tile() { tileName = tile.tileName, tileTypes = new Tiledata.TileType[] { tile.tileTypeData } }, type, optionsObject, defaultAsset, 0);
        }

        public void DrawTile(Tiledata.Tile tile, int type, Options1 optionsObject, int sizeMultiplier, int currentX, int currentY, SavedImages imageMemory, float[] borderSize) // Drawing a tile (normal)
        {
            var real = GetRealAsset(tile, type, optionsObject, tile.tileTypes[type].asset);

            int offTop = (int)Math.Round((double)real.tileParts.top * sizeMultiplier / 1000);
            int offLeft = (int)Math.Round((double)real.tileParts.left * sizeMultiplier / 1000);

            var ti = imageMemory.GetTileImage(real);

            g.DrawImage(ti.renderedImage, (int)Math.Round(sizeMultiplier * (currentX + borderSize[2])) - offLeft, (int)Math.Round(sizeMultiplier * (currentY + borderSize[0])) - offTop);
            return;
        }

        public void DrawSelectedTile(OrderedTile tile, Options1 optionsObject, int sizeMultiplier, SavedImages imageMemory, float[] borderSize) // Drawing a tile (with saved coordinates and a pre-selected type)
        {
            var real = GetRealAsset(tile, tile.tileType, optionsObject, tile.tileTypeData.asset);

            int offTop = (int)Math.Round((double)real.tileParts.top * sizeMultiplier / 1000);
            int offLeft = (int)Math.Round((double)real.tileParts.left * sizeMultiplier / 1000);

            var ti = imageMemory.GetTileImage(real);

            g.DrawImage(ti.renderedImage, (int)Math.Round(sizeMultiplier * (tile.xPosition + borderSize[2])) - offLeft, (int)Math.Round(sizeMultiplier * (tile.yPosition + borderSize[0])) - offTop);
            return;
        }

        public void ColorBackground(Tiledata tiledata, Options1.BatchSettings batchOption, float[] borderSize) // Filling in background colors
        {
            AMGState.drawer.ResetCursor();

            var bg = tiledata.biomes[batchOption.biome].background;

            if (bg == null)
                bg = tiledata.defaultBiome.background;

            object result;

            while (!AMGState.map.drawn)
            {
                if (batchOption.skipTiles.Contains(AMGState.ReadAtCursor()))
                {
                    AMGState.MoveCursor();
                    continue;
                }

                if (bg.parameters != null && bg.parameters.Count > 0)
                    result = tiledata.BackgroundManagerInstance.RunFunction(bg.name, bg.parameters);
                else
                    result = tiledata.BackgroundManagerInstance.RunFunction(bg.name);

                if (result is ColorData color)
                {
                    g.FillRectangle(
                        new SolidBrush(Color.FromArgb(color.r, color.g, color.b)),
                        (int)Math.Round(batchOption.sizeMultiplier * (AMGState.drawer.cursor.x + borderSize[2])),
                        (int)Math.Round(batchOption.sizeMultiplier * (AMGState.drawer.cursor.y + borderSize[0])),
                        batchOption.sizeMultiplier, batchOption.sizeMultiplier
                    );
                    AMGState.MoveCursor();
                }
                else
                    throw new ApplicationException("Expected a Color output from background blocks, but received " + result.GetType().ToString());
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

        public static string tileLinks(string[] map, int currentX, int currentY, Tiledata.Tile tileObject, Options1.Replace[] replaces)
        {
            string binary = "";
            string neighbors;
            if (currentY == 0 && currentY == map.Length - 1) // One line
            {
                if (currentX == 0 && currentX == map[0].Length - 1)
                    neighbors = "%%%%%%%%"; // One column
                else if (currentX == 0)
                    neighbors = "%%%% %%%"; // Left
                else if (currentX == map[0].Length - 1)
                    neighbors = "%%% %%%%"; // Right
                else
                    neighbors = "%%%  %%%"; // Middle
            }
            else if (currentY == 0) // Top
            {
                if (currentX == 0 && currentX == map[0].Length - 1)
                    neighbors = "%%%%%% %"; // One column
                else if (currentX == 0)
                    neighbors = "%%%% %  "; // Left
                else if (currentX == map[0].Length - 1)
                    neighbors = "%%% %  %"; // Right
                else
                    neighbors = "%%%     "; // Middle
            }
            else if (currentY == map.Length - 1) // Bottom
            {
                if (currentX == 0 && currentX == map[0].Length - 1)
                    neighbors = "% %%%%%%"; // One column
                else if (currentX == 0)
                    neighbors = "%  % %%%"; // Left
                else if (currentX == map[0].Length - 1)
                    neighbors = "  % %%%%"; // Right
                else
                    neighbors = "     %%%"; // Middle
            }
            else // Middle
            {
                if (currentX == 0 && currentX == map[0].Length - 1)
                    neighbors = "% %%%% %"; // One column
                else if (currentX == 0)
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
                            binary += '0'; // edgeCase
                        else
                            binary += checkNeighboringTile(map, currentX, currentY, tileObject, replaces, x); // check tile in map
                    }
                    break;
                case Tiledata.EdgeCase.copies: // edges are filled with equal tiles
                    for (int x = 0; x < neighbors.Length; x++)
                    {
                        if (neighbors[x] == '%')
                            binary += '1'; // edgeCase
                        else
                            binary += checkNeighboringTile(map, currentX, currentY, tileObject, replaces, x); // check tile in map
                    }
                    break;
                case Tiledata.EdgeCase.mirror: // edges are extended
                    for (int x = 0; x < neighbors.Length; x++)
                    {
                        if (neighbors[x] == '%')
                            if (x % 2 == 1)
                                binary += '1'; // edgeCase (Edge adjacent edge tiles will always be equal when extending)
                            else
                            {
                                if (hasAdjacentEqualTiles(map, currentX - 1, currentY - 1, tileObject))
                                    binary += '1';
                                else if (hasAdjacentEqualTiles(map, currentX - 1, currentY + 1, tileObject))
                                    binary += '1';
                                else if (hasAdjacentEqualTiles(map, currentX + 1, currentY - 1, tileObject))
                                    binary += '1';
                                else if (hasAdjacentEqualTiles(map, currentX + 1, currentY + 1, tileObject))
                                    binary += '1';
                                else
                                    binary += '0';
                            }
                        // binary = binary + checkNeighboringTile(map, currentX, currentY, tileObject, 7 - x); // edgeCase (check opposite tile to extend)
                        else
                            binary += checkNeighboringTile(map, currentX, currentY, tileObject, replaces, x); // check tile in map
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
    }

    public static class Logger
    {
        private static Options1 savedOptionsObject = new Options1 { console = new Options1.ConsoleOptions() { aal = true, orderedHorTileDraw = true, orderedTileDraw = true, saveLocation = true, setup = true, tileDraw = true }, saveLogFile = true };
        public static TitleClass Title = new TitleClass();
        public static string version;

        public static void UpdateOptions(Options1 optionsObject, string version)
        {
            savedOptionsObject = optionsObject;
            Title.UpdateObjects(optionsObject.title);
            Title.version = version;
            Console.Title = Title.GetAppInfo();
        }

        static List<string> loggedLines = new List<string>();

        public static void Log(string text) // Send a line to console + add to log
        {
            Console.WriteLine(text);
            loggedLines.Add(text);
        }

        public static void LogSpacer() // Empty line
        {
            Console.WriteLine();
            loggedLines.Add("");
        }

        public enum AALDirection { In, Out }
        public static void LogAAL(AALDirection direction, string file) // Log AAL events
        {
            if (!savedOptionsObject.console.aal) return;
            if (direction == AALDirection.In)
                Log(" [ AAL ] READ << " + file);
            else
                Log(" [ AAL ] WRITE >> " + file);
        }

        public static void LogStatus(string text) // Log status changes
        {
            if (!savedOptionsObject.console.statusChange) return;
            Log(" Status: " + text);
        }

        public static void LogSetup(string text, bool prefix = true) // Log setup jobs
        {
            if (!savedOptionsObject.console.setup) return;
            if (prefix)
                Log("New job: " + text);
            else
                Log(text);
        }

        public static void LogExport(string file) // Log setup jobs
        {
            if (!savedOptionsObject.console.saveLocation) return;
            LogSpacer();
            if (Regex.IsMatch(file, "\\S:"))
                Log("Image saved!\n  Location: " + file);
            else
                Log("Image saved!\n  Location: " + Path.GetFullPath("./" + file));
        }

        public enum TileEvent { tileDraw, orderedHorTileDraw, orderedTileDraw, gamemodeModding }

        public static void LogTile(TileActionTypes tat, Tiledata.Tile tile, int y, int x, int yMax, int xMax, TileEvent tileEvent) // Log tile events
        {
            LogTile(tat, tile, y.ToString(), x.ToString(), yMax, xMax, tileEvent);
        }

        public static void LogTile(TileActionTypes tat, Tiledata.Tile tile, string y, string x, int yMax, int xMax, TileEvent tileEvent) // Log tile events
        {
            if (tileEvent == TileEvent.tileDraw && !savedOptionsObject.console.tileDraw) return;
            if (tileEvent == TileEvent.orderedHorTileDraw && !savedOptionsObject.console.orderedHorTileDraw) return;
            if (tileEvent == TileEvent.orderedTileDraw && !savedOptionsObject.console.orderedTileDraw) return;
            if (tileEvent == TileEvent.gamemodeModding && !savedOptionsObject.console.gamemodeModding) return;

            Log(TileActionStringMaker(tat, tile, y, x, yMax, xMax));
        }

        public static void LogWarning(string text, int timeout = 10) // Log a warning and pause
        {
            LogSpacer();
            Log(" WARNING: " + text);
            Log(string.Format(" Resuming in {0} seconds...", timeout));
            Thread.Sleep(timeout * 1000);
        }

        public static void LogError(Exception error) // Log an error
        {
            LogError(error.ToString());
        }

        public static void LogError(string error) // Log an error
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

        public static void Save(string fileName) // Save log file
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

        public static string TileActionStringMaker(TileActionTypes tat, Tiledata.Tile tile, int yLocation, int xLocation, int yLocationMax, int xLocationMax) // Text maker for a voice when the generator is doing actions related to tiles
        {
            return TileActionStringMaker(tat, tile, yLocation.ToString(), xLocation.ToString(), yLocationMax, xLocationMax);
        }

        public static string TileActionStringMaker(TileActionTypes tat, Tiledata.Tile tile, string yLocation, string xLocation, int yLocationMax, int xLocationMax) // Text maker for a voice when the generator is doing actions related to tiles
        {
            string p;
            string t;
            string n = tile.tileName.ToUpper();

            if (tat.m) p = "m"; else p = " ";
            if (tat.g) p += "g"; else p += " ";
            if (tat.s) p += "s"; else p += " ";
            if (tat.o) p += "o"; else p += " ";
            if (tat.h) p += "h"; else p += " ";
            if (tat.d) p += "d"; else p += " ";

            if (tat.m)
            {
                if (tat.d)
                    t = "DRAWN METADATA TILE AS \"" + n + "\".";
                else
                    t = "REGISTERED METADATA TILE \"" + n + "\".";
            }
            else if (tat.g)
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
    }

    public static class AMGState
    {
        public class Vector2
        {
            public int x = 0;
            public int y = 0;

            public Vector2() { }
            public Vector2(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        public class Drawer
        {
            public void ResetCursor()
            {
                cursor.x = 0;
                cursor.y = 0;
            }

            public Vector2 cursor = new Vector2();

            public static int _tilesDrawn = 0;
            public int tilesDrawn => _tilesDrawn;
            public void DrawnTile() => _tilesDrawn++;

            public static int _mapsDrawn = 0;
            public int mapsDrawn => _mapsDrawn;
            public void DrawnMap() => _tilesDrawn++;
        }

        public class Map
        {
            public class Size
            {
                public int height;
                public int width;

                public Vector2 middle;

                public Size(int height, int width)
                {
                    this.height = height;
                    this.width = width;

                    middle = new Vector2(
                        width / 2 - (width + 1) % 2,
                        height / 2 - (height + 1) % 2
                        );
                }
            }

            public Size size => new Size(data.Length, data[0].Length);

            public string[] data;

            private static int _index = -1;
            public int index => _index;

            public bool valid = true;
            public bool drawn = false;

            public Map() { }
            public Map(string[] data)
            {
                this.data = data;
                _index++;
            }
            public Map(Options1.BatchSettings batch)
            {
                data = batch.map;

                if (batch.map.Length == 0 || batch.map[0].Length == 0)
                {
                    Logger.LogWarning("Map is empty!\n  [Object] Map in the index number " + index + " has no string arrays.", 4);
                    valid = false;
                }

                _index++;
            }

            public void AutoCrop(char[] tiles)
            {
                Size s = size;
                int t, b, l = s.width, r = 0;
                string line;

                for (t = 0; t < s.height; t++)
                {
                    line = data[t];
                    foreach (char c in tiles)
                        line = line.Replace(c.ToString(), string.Empty);
                    if (line != string.Empty)
                        break;
                }

                for (b = s.height - 1; b >= 0; b--)
                {
                    line = data[b];
                    foreach (char c in tiles)
                        line = line.Replace(c.ToString(), string.Empty);
                    if (line != string.Empty)
                        break;
                }

                for (int e = t; e <= b; e++)
                {
                    line = data[e];
                    if (line.Length - line.TrimStart(tiles).Length < l)
                        l = line.Length - line.TrimStart(tiles).Length;
                    if (line.Length - line.TrimEnd(tiles).Length < r)
                        r = line.Length - line.TrimEnd(tiles).Length;
                }

                if (t != 0 || b != s.height - 1 || l != 0 || r != 0)
                {
                    data = data
                        .Skip(t)
                        .Take(b - t + 1)
                        .Select(item => item.Substring(l, item.Length - l - r))
                        .ToArray();

                    Logger.LogSpacer();
                    Logger.LogSetup("Auto-Cropped map:", false);
                    Logger.LogSetup(string.Format(
                        "  {0} Top\n  {1} Bottom\n  {2} Left\n  {3} Right",
                        t, s.height - b - 1, l, r
                    ), false);
                }
            }
        }

        public class Version
        {
            public int major;
            public int minor;
            public int patch;
            public string access;

            public Version(int major, int minor, int patch, string access)
            {
                this.major = major;
                this.minor = minor;
                this.patch = patch;
                this.access = access;
            }

            public override string ToString()
            {
                return string.Format("v{0}.{1}.{2} {3}", major, minor, patch, access);
            }
        }

        private static Stopwatch _stopwatch = new Stopwatch();

        public static Drawer drawer = new Drawer();
        public static Map map = new Map();
        public static Version version = new Version(1, 9, 5, "Release");

        public static void NewMap(Options1.BatchSettings batch)
        {
            drawer = new Drawer();
            map = new Map(batch);
        }

        public static void MoveCursor()
        {
            drawer.cursor.x++;
            if (drawer.cursor.x >= map.size.width)
            {
                drawer.cursor.x = 0;
                drawer.cursor.y++;
                if (drawer.cursor.y >= map.size.height)
                {
                    drawer.cursor.y = 0;
                    map.drawn = true;
                }
            }
        }

        public static char ReadAtCursor()
        {
            return map.data[drawer.cursor.y][drawer.cursor.x];
        }

        public static void StartTimer() => _stopwatch.Start();
        public static void StopTimer() => _stopwatch.Stop();

        public static long time => _stopwatch.ElapsedMilliseconds;

        public static float GetNumber(string name)
        {
            switch (name)
            {
                case "MAP->INDEX":
                    return map.index;

                case "MAP->SIZE->WIDTH":
                    return map.size.width;
                case "MAP->SIZE->HEIGHT":
                    return map.size.height;

                case "MAP->SIZE->MIDDLE->X":
                    return map.size.middle.x;
                case "MAP->SIZE->MIDDLE->Y":
                    return map.size.middle.y;

                case "DRAWER->CURSOR->X":
                    return drawer.cursor.x;
                case "DRAWER->CURSOR->Y":
                    return drawer.cursor.y;

                default:
                    throw new ApplicationException("Number of key '" + name + "' does not exist");
            }
        }
    }

}
