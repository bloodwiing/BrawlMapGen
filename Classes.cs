using System;
using System.Collections.Generic;
using System.Text;

namespace BMG
{
    public class Preset
    {
        public string display { get; set; }
        public string author { get; set; }
        public string social { get; set; }
        public int required { get; set; }
        public char[] ignoreTiles { get; set; }
        public Offset defaultOffset { get; set; }
    }

    public class Asset
    {
        public Offset offset { get; set; }
    }

    public class Offset
    {
        public int top { get; set; }
        public int mid { get; set; }
        public int bot { get; set; }
        public int left { get; set; }
        public int right { get; set; }
    }
}
