using System;
using System.Collections.Generic;

namespace BMG.Cache
{
    public class CacheInstance
    {
        private readonly Dictionary<string, TileCache> tileImages = new Dictionary<string, TileCache>();

        private Random rnd;

        private readonly OptionsBase options;
        private readonly int scale;


        public CacheInstance(OptionsBase options, int scale)
        {
            // SAVE INSTANCE OPTIONS

            this.options = options;
            this.scale = scale;


            // SET SEED IF POSSIBLE

            if (options.RandomizerSeed != null)
                rnd = new Random(options.RandomizerSeed.Value);
            else
                rnd = new Random();
        }


        public void SetRandomSeed(int seed)
        {
            rnd = new Random(seed);
        }


        public TileCache GetImage(GraphicLayer layer)
        {
            // GET ORIGINAL TILE

            string asset = layer.Asset;

            /*
            // RANDOMIZE TILE [TEMP]

            TileAssetBase final = tile;

            if (tile.Randomizer != null && options.HasRandomizer)
            {
                final = tile.Randomizer[rnd.Next(tile.Randomizer.Length)];
                asset = final.Asset;

                //if (final.Offset == null)
                //    final.Offset = tile.Offset;
            }
            */


            // IF ALREADY CACHED -> RETURN CACHE

            if (tileImages.ContainsKey(asset))
                return tileImages[asset];


            // IF NOT -> MAKE CACHE

            var instance = new TileCache(options, scale, layer);
            tileImages.Add(asset, instance);
            return instance;
        }
    }
}
