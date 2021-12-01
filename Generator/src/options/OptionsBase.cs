using BMG.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BMG
{
    public abstract class OptionsBase
    {
        // BASE

        public abstract string WorkingDir { get; set; }
        public abstract string Preset { get; }
        public abstract bool SaveLog { get; }
        public abstract string Output { get; }


        // MAPS

        public abstract IMap[] Maps { get; set; }
        public IMap GetMap(int index) { return Maps[index]; }
        public int MapCount => Maps.Length;

        public abstract MapOptimizerBase MapOptimizer { get; }
        public abstract void OptimizeMaps();


        // AUTO CROP

        public abstract bool HasAutoCrop { get; set; }
        public abstract char[] AutoCrop { get; set; }


        // OPTIONS

        public abstract ConsoleOptionsBase ConsoleOpts { get; }
        public abstract TitleOptionsBase TitleOpts { get; }


        // RANDOMIZER

        public abstract bool HasRandomizer { get; }
        public abstract int? RandomizerSeed { get; }
    }
}
