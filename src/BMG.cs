using AMGBlocks;
using BMG.State;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace BMG
{
    class BMG
    {
        readonly OptionsBase options;
        public PresetBase preset { get; private set; }

        readonly Dictionary<char, int> tilesFailed = new Dictionary<char, int>();
        readonly Dictionary<int, SavedImages> savedTileImageList = new Dictionary<int, SavedImages>();


        public BMG(OptionsBase options)
        {
            this.options = options;
        }


        public void Run()
        {
            //try
            //{
                AMGState.StartTimer();

                Intro();  // Intro text
                LoadPreset();  // File loading
                OptimizeMaps();  // Incl. & Excl.
                Prepare();  // Fallback
                DrawMaps();  // Generation
            //}
            //catch (Exception e)
            //{
            //    Logger.LogError(e);
            //}
        }

        
        private void Intro()
        {
            Logger.UpdateOptions(options, AMGState.version.major + "." + AMGState.version.minor + "." + AMGState.version.patch);

            if (options.WorkingDir != null)
                Environment.CurrentDirectory = options.WorkingDir;

            // CREDITS

            Logger.LogSpacer();
            Logger.Log("  BMG (Brawl Map Gen)");
            Logger.Log("    Created by: BloodWiing");
            Logger.Log("    Helped by: 4JR, Henry, tryso");


            // VERSIONS

            Logger.LogSpacer();
            Logger.Log("  VERSION");
            Logger.Log($"    BrawlMapGen -- {AMGState.version}");
            Logger.Log($"    AMG!Blocks -- {AMGBlocks.AMGBlocks.version}");


            // STATUS

            Logger.LogSpacer();
            Logger.LogSpacer();
            Logger.LogStatus("APP is launched!");
            Logger.LogSetup($"Loading PRESET: \"{options.Preset.ToUpper()}\"...");
        }


        private void LoadPreset()
        {
            // CHECK FILE

            Logger.Title.Job.UpdateJob(0, 1, "Preparing...");
            Logger.Title.Status.UpdateStatus(0, 1, "Loading PRESET...");
            Logger.Title.UpdateStatusDetails(options.Preset.ToUpper(), Logger.TitleClass.StatusDetailsType.basic);
            Logger.Title.RefreshTitle();

            string fileloc = $"./presets/{options.Preset}.json";

            if (!File.Exists(fileloc))
            {
                Logger.LogError($"PRESET doesn't exist\n  [FileReader] Unable to find file in location \"{fileloc}\"");
                Logger.Save("log.txt");
                Thread.Sleep(3000);
                Environment.Exit(1);
            }


            // READ FILE

            Logger.LogAAL(Logger.AALDirection.In, fileloc);
            StreamReader reader = new StreamReader(fileloc);
            string json = reader.ReadToEnd();
            reader.Close();


            // SET PRESET

            preset = JsonConvert.DeserializeObject<PresetOld>(json, new AMGBlockReader());

            Logger.LogSetup($"PRESET loaded: \"{options.Preset.ToUpper()}\"!", false);
            Logger.LogStatus($"All assets will be loading according to the \"{options.Preset.ToUpper()}\" PRESET.");
            Logger.LogSpacer();
        }


        private void OptimizeMaps()
        {
            // EXCLUDE AND INCLUDE

            Logger.Title.Job.UpdateJob(0, 1, "Gathering optimization data...");
            Logger.Title.RefreshTitle();
            Logger.LogStatus("Checking INCLUSIONS and EXCLUSIONS.");

            MapOptimizerBase optimizer = options.MapOptimizer;
            options.OptimizeMaps();

            Logger.LogStatus("Maps are optimized!");

            if (optimizer.Inclusions.Length > 0)
                Logger.LogSetup("INCLUSIONS: " + string.Join(", ", optimizer.Inclusions) + ".", false);
            if (optimizer.Exclusions.Length > 0)
                Logger.LogSetup("EXCLUSIONS: " + string.Join(", ", optimizer.Exclusions) + ".", false);

            Logger.LogSpacer();
        }


        private void Prepare()
        {
            // MAP ARRAY CHECK

            if (options.MapCount == 0)
                throw new ArgumentException("Missing Maps from OPTIONS");


            // AUTO CROP FALLBACK

            if (options.HasAutoCrop && options.AutoCrop.Length == 0)
            {
                options.AutoCrop = options.GetMap(0).VoidTiles;
                Logger.LogWarning("AUTO CROP is enabled, but empty. Defaulting to OPTIONS' first map's VOID TILES.\n Please make sure to update this setting next time", 15);
                Thread.Sleep(10000);
            }
        }


        private void DrawMaps()
        {
            Logger.LogStatus("Map image generator starting...");
            Logger.Title.Job.UpdateJob(0, options.MapCount);

            foreach (MapBase map in options.Maps)
            {
                string mapName = map.GetName();

                Logger.Title.Job.UpdateJobName($"\"{mapName}\"");
                Logger.Title.Job.IncreaseJob();


                // LOAD IMAGE CACHE FOR SCALE

                SavedImages selectedTileImageList;
                if (!savedTileImageList.ContainsKey(map.Scale))
                {
                    selectedTileImageList = new SavedImages(options, map.Scale);
                    savedTileImageList.Add(map.Scale, selectedTileImageList);
                }
                else
                    selectedTileImageList = savedTileImageList[map.Scale];


                // SET SEED

                if (map.GenerationSeed != null)
                    selectedTileImageList.SetRandomSeed(map.GenerationSeed.GetValueOrDefault());


                // MAP VALIDATION

                Logger.LogSpacer();
                Logger.LogStatus("Getting DATA...");
                Logger.LogSetup($"Looking for DATA in {mapName}...");

                if (map.Data == null)
                {
                    Logger.LogWarning($"  DATA is empty!\n  [Object] DATA of MAP {mapName} is not defined.", 4);
                    continue;
                }

                Logger.LogSetup("  DATA found.", false);
                Logger.LogStatus("DATA read.");


                // AUTO CROPPING

                if (options.HasAutoCrop)
                    map.AutoCrop(options.AutoCrop);

                AMGState.NewMap(map);

                if (!AMGState.map.valid)
                    continue;


                // GENERATION

                GenerateMap(map);
            }
        }


        private void GenerateMap(MapBase map)
        {
            // GET BIOME

            BiomeBase mapBiome = preset.GetBiome(map.Biome);


            // SETUP RENDERER

            Renderer renderer = new Renderer(map, options, preset);

            Logger.LogSpacer();
            Logger.Log($"Map details:\n  Width: {renderer.CanvasWidth}px\n  Height: {renderer.CanvasHeight}px\n  Biome: \"{mapBiome.Name}\"\n");


            // RENDER

            MakeBackground(renderer, map);


            renderer.ExportImage();
        }


        private void MakeBackground(Renderer renderer, MapBase map)
        {
            // DRAW BACKGROUND

            Logger.LogSetup("Drawing background...");
            Logger.LogStatus("Running AMG!Blocks...");

            renderer.ColorBackground(map);

            Logger.LogStatus("Background drawn.");
        }
    }
}
