using System.Collections.Generic;

namespace BMG.Cache
{
    public class CacheManager
    {
        readonly static Dictionary<int, CacheInstance> instances = new Dictionary<int, CacheInstance>();

        public static CacheInstance GetInstance(OptionsBase options, int scale)
        {
            if (!instances.TryGetValue(scale, out CacheInstance instance))
            {
                instance = new CacheInstance(options, scale);
                instances.Add(scale, instance);
            }
            return instance;
        }
    }
}
