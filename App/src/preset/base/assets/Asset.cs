using Idle.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace BMG
{
    public class Asset
    {
        [IdleProperty("ASSET")]
        public string Name { get; protected set; }

        [IdleProperty("VARIANT")]
        AssetVariant[] _variants { set => Variants = value.ToDictionary(x => x.ID, x => (AssetVariant)x); }
        protected Dictionary<string, AssetVariant> Variants { get; set; }

        [IdleProperty("SELECTOR")]
        protected Selector Selector { get; set; }

        public AssetVariant ChooseVariant(byte neighbors)
        {
            string asset = Selector.Choose(neighbors);

            if (Variants.TryGetValue(asset, out AssetVariant variant))
                return variant;

            return Variants.Values.First();
        }
    }
}
