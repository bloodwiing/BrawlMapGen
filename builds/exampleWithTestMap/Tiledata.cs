using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace generator
{
    class Tiledata
    {

        public Tile[] tiles { get; set; }
        public Biome[] biomes { get; set; }

        public class Tile
        {
            public string tileName { get; set; }
            public char tileCode { get; set; }
            public TileType[] tileTypes { get; set; }
            public TileLink tileLinks { get; set; }
        }

        public class TileType
        {
            public TileParts tileParts { get; set; }
            public string color { get; set; }
            public bool detailed { get; set; }
            public bool visible { get; set; }
            public string other { get; set; }
            public string asset { get; set; }
            public int? orderHor { get; set; }
            public int? order { get; set; }
        }

        public class TileParts
        {
            public int top { get; set; }
            public int mid { get; set; }
            public int bot { get; set; }
            public int left { get; set; }
            public int right { get; set; }
        }

        public class TileDefault
        {
            public string tile { get; set; }
            public int type { get; set; }
        }

        public class Biome
        {
            public string name { get; set; }
            public string color1 { get; set; }
            public string color2 { get; set; }
            public TileDefault[] defaults { get; set; }
        }

        public class TileLink
        {
            public TileLinkRule[] rules { get; set; }
            public TileLinkDefault defaults { get; set; }
            public string assetFolder { get; set; }
        }

        public class TileLinkRule
        {
            public string condition { get; set; }
            public string[] changeBinary { get; set; }
            public int? changeTileType { get; set; }
            public string changeAsset { get; set; }
        }

        public class TileLinkDefault
        {
            public int tileType { get; set; }
            public string asset { get; set; }
        }

    }

}
