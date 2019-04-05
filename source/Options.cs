using System.Collections.Generic;

namespace BMG
{
    public class Options
    {

        public string setPath { get; set; }
        public string preset { get; set; }
        public BatchSettings[] batch { get; set; }
        public string exportFileName { get; set; } = "bmg_?number?.png";
        public string exportFolderName { get; set; } = "output";
        public bool saveLogFile { get; set; } = true;
        public ConsoleOptions console { get; set; }
        public Title title { get; set; }

        public class Replace
        {
            public char from { get; set; }
            public char to { get; set; }
        }

        public class BatchSettings
        {
            public string name { get; set; } = "?number?";
            public string[] map { get; set; }
            public int biome { get; set; }
            public int sizeMultiplier { get; set; }
            public char[] skipTiles { get; set; }
            public Replace[] replaceTiles { get; set; }
            public string exportFileName { get; set; }
            public Tiledata.TileDefault[] overrideBiome { get; set; }
            public SpecialTileRules[] specialTileRules { get; set; }
            public string gamemode { get; set; }
        }

        public class ConsoleOptions
        {
            public bool setup { get; set; } = true;
            public bool tileDraw { get; set; } = true;
            public bool orderedHorTileDraw { get; set; } = true;
            public bool orderedTileDraw { get; set; } = true;
            public bool saveLocation { get; set; } = true;
            public bool aal { get; set; } = true;
            public bool statusChange { get; set; } = true;
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
                tileTime = 1
            });
        }

    }

}
