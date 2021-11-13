using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BMG
{
    public class OptionsOld : OptionsBase
    {
        // IMPLEMENTATIONS

        public override string WorkingDir { get => setPath; set => setPath = value; }
        public override string Preset => preset;
        public override bool SaveLog => saveLogFile;
        public override string Output => exportFolderName;

        public override MapBase[] Maps { get => batch; set => batch = (BatchSettings[])value; }

        public override MapOptimizerBase MapOptimizer => render;
        public override void OptimizeMaps()
        {
            List<BatchSettings> final = new List<BatchSettings>();
            for (int i = 0; i < batch.Length; i++)
                if ((render.include.Length == 0 || render.include.Contains(i)) // Include check
                    && (render.exclude.Length == 0 || !render.exclude.Contains(i))) // Exclude check
                    final.Add(batch[i]);
            batch = final.ToArray();
        }

        public override bool HasAutoCrop { get => autoCrop.enabled; set => autoCrop.enabled = value; }
        public override char[] AutoCrop { get => autoCrop.tiles; set => autoCrop.tiles = value; }

        public override ConsoleOptionsBase ConsoleOpts => console;
        public override TitleOptionsBase TitleOpts => title;

        public override bool HasRandomizer => randomizers.enabled;
        public override int? RandomizerSeed => randomizers.seed;


        public string setPath { get; set; }
        public string preset { get; set; }
        public BatchSettings[] batch { get; set; }
        public string exportFileName { get; set; } = "bmg_?number?.png";
        public string exportFolderName { get; set; } = "output";
        public bool saveLogFile { get; set; } = true;
        public ConsoleOptions console { get; set; } = new ConsoleOptions();
        public Title title { get; set; } = new Title();
        public Render render { get; set; } = new Render();
        public _AutoCrop autoCrop { get; set; } = new _AutoCrop();
        public AssetSwitcher[] assetSwitchers { get; set; }
        public Randomizers randomizers { get; set; } = new Randomizers();

        public class Replace
        {
            public char from { get; set; }
            public char to { get; set; }
        }

        public class Override
        {
            public string tile { get; set; }
            public int type { get; set; }
        }

        public class BatchSettings : MapBase
        {

            // IMPLEMENTATIONS

            public override string RawName { get => name; set => name = value; }
            public override string[] Data { get => map; set => map = value; }
            public override object Biome => biome;
            public override int Scale { get => sizeMultiplier; set => sizeMultiplier = value; }
            public override char[] VoidTiles => skipTiles;

            public override Dictionary<string, int> BiomeOverrides => _overrideDict;


            public override Margin Margin
            {
                get
                {
                    float t, b, l, r;

                    switch (emptyBorderAmount.Length)
                    {
                        case 0:
                            t = b = l = r = 1;
                            break;
                        case 1:
                            t = b = l = r = emptyBorderAmount[0];
                            break;
                        case 2:
                            t = b = emptyBorderAmount[0];
                            l = r = emptyBorderAmount[1];
                            break;
                        case 3:
                            t = emptyBorderAmount[0];
                            b = emptyBorderAmount[1];
                            l = r = emptyBorderAmount[2];
                            break;
                        case 4:
                            t = emptyBorderAmount[0];
                            b = emptyBorderAmount[1];
                            l = emptyBorderAmount[2];
                            r = emptyBorderAmount[3];
                            break;
                        default:
                            throw new ApplicationException("MARGIN must have from 0 to 4 elements only!");
                    }

                    return new Margin(t, b, l, r);
                }
            }

            public override string GameMode => gamemode;

            public override int? GenerationSeed => randomSeed;


            [OnDeserialized]
            internal void Prepare(StreamingContext context)
            {
                foreach (var @override in overrideBiome)
                    _overrideDict.TryAdd(@override.tile, @override.type);
            }


            Dictionary<string, int> _overrideDict = new Dictionary<string, int>();


            public string name { get; set; } = "MAP_{INDEX}";
            public string[] map { get; set; }
            public object biome { get; set; }
            public int sizeMultiplier { get; set; }
            public char[] skipTiles { get; set; } = new char[0];
            public Replace[] replaceTiles { get; set; }
            public string exportFileName { get; set; }
            public Override[] overrideBiome { get; set; } = new Override[0];
            public SpecialTileRules[] specialTileRules { get; set; }
            public float[] emptyBorderAmount { get; set; } = new float[] { 1 };
            public string gamemode { get; set; }
            public int? randomSeed { get; set; }
            public Dictionary<string, Metadata[]> mapMetadata { get; set; }
        }

        public class ConsoleOptions : ConsoleOptionsBase
        {
            // IMPLEMENTATIONS

            public override BMGEvent EventFilter => _filter;


            [OnDeserialized]
            internal void Prepare(StreamingContext context)
            {
                _filter = 0;
                if (setup) _filter |= BMGEvent.SETUP;
                if (tileDraw) _filter |= BMGEvent.DRAW;
                if (saveLocation) _filter |= BMGEvent.EXPORT;
                if (aal) _filter |= BMGEvent.AAL;
                if (statusChange) _filter |= BMGEvent.STATUS;
                if (gamemodeModding) _filter |= BMGEvent.MOD;
            }


            private BMGEvent _filter;

            public bool setup { get; set; } = true;
            public bool tileDraw { get; set; } = true;
            public bool saveLocation { get; set; } = true;
            public bool aal { get; set; } = true;
            public bool statusChange { get; set; } = true;
            public bool gamemodeModding { get; set; } = true;
        }

        public class SpecialTileRules
        {
            public char tileCode { get; set; }
            public int tileTime { get; set; }
            public int tileType { get; set; }
        }

        public class RecordedSTR
        {
            public char tileCode { get; set; }
            public int tileTime { get; set; }
        }

        public static void RecordRSTR(List<RecordedSTR> rstrArray, char tileCode)
        {
            foreach (var rstro in rstrArray)
                if (rstro.tileCode == tileCode)
                {
                    rstro.tileTime++;
                    return;
                }

            rstrArray.Add(new RecordedSTR()
            {
                tileCode = tileCode,
                tileTime = 0
            });
        }

        public class AppInfo : TitleOptionsBase.AppInfoBase
        {
            // IMPLEMENTATIONS

            public override bool ShowVersion => showVersion;


            public bool showVersion { get; set; } = true;
        }

        public class Job : TitleOptionsBase.JobBase
        {
            // IMPLEMENTATIONS

            public override char Full => percentageBarFillCharacter;
            public override char Empty => percentageBarBackgroundCharacter;
            public override string Layout => order;


            public char percentageBarFillCharacter { get; set; } = '#';
            public char percentageBarBackgroundCharacter { get; set; } = '-';
            public string order { get; set; } = "{PERCENT} [{BAR}] {JOB} {RATIO}";
        }

        public class Status : TitleOptionsBase.StatusBase
        {
            // IMPLEMENTATIONS

            public override char Full => percentageBarFillCharacter;
            public override char Empty => percentageBarBackgroundCharacter;
            public override string Layout => order;


            public char percentageBarFillCharacter { get; set; } = '#';
            public char percentageBarBackgroundCharacter { get; set; } = '-';
            public string order { get; set; } = "{PERCENT} [{BAR}] {STATUS} {RATIO}";
        }

        public class StatusDetails : TitleOptionsBase.StatusDetailsBase
        {
            // IMPLEMENTATIONS

            public override bool ShowBiome => showBiome;
            public override bool ShowTile => showTile;


            public bool showBiome { get; set; } = true;
            public bool showTile { get; set; } = true;
        }

        public class Modules
        {
            public AppInfo appInfo { get; set; } = new AppInfo();
            public Job job { get; set; } = new Job();
            public Status status { get; set; } = new Status();
            public StatusDetails statusDetails { get; set; } = new StatusDetails();
        }

        public class Title : TitleOptionsBase
        {
            // IMPLEMENTATIONS

            public override AppInfoBase AppInfo => modules.appInfo;
            public override JobBase Job => modules.job;
            public override StatusBase Status => modules.status;
            public override StatusDetailsBase StatusDetails => modules.statusDetails;
            public override string Layout => layout;
            public override bool UpdateEnabled => !disableUpdate;


            public Modules modules { get; set; } = new Modules();
            public string layout { get; set; } = "{APP} - {JOB} - {STATUS} - {DETAILS}";
            public bool disableUpdate { get; set; } = false;
        }

        public class Render : MapOptimizerBase
        {
            // IMPLEMENTATIONS

            public override int[] Inclusions => include;
            public override int[] Exclusions => exclude;


            public int[] include { get; set; } = { };
            public int[] exclude { get; set; } = { };
        }

        public class _AutoCrop
        {
            public bool enabled { get; set; } = false;
            public char[] tiles { get; set; } = { };
        }

        public class AssetSwitcher
        {
            public TileAsset find { get; set; }
            public TileAsset replace { get; set; }
        }

        public class TileAsset
        {
            public string tile { get; set; }
            public int type { get; set; }
        }

        public class Randomizers
        {
            public bool enabled { get; set; } = true;
            public int? seed { get; set; }
        }

        public class Metadata
        {
            public int x { get; set; }
            public int y { get; set; }
            public int t { get; set; }
        }

    }

}
