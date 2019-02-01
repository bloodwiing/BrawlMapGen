using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace generator
{
    class Options
    {

        public string preset { get; set; }
        public string[] map { get; set; }
        public int biome { get; set; }
        public int sizeMultiplier { get; set; }
        public Replace[] replaceTiles { get; set; }
        public string exportFileName { get; set; }
        public bool saveLogFile { get; set; }

        public class Replace
        {
            public char from { get; set; }
            public char to { get; set; }
        }

    }

}
