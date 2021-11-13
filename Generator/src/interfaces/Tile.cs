using System;
using System.Xml.Serialization;

namespace BMG.Preset.New
{
    public interface ITile
    {
        string Name { get; }
        char Code { get; }

        ITileVariant[] Variants { get; }

        ITileVariant GetVariant(int variant);
        ITileVariant GetVariant(BiomeBase biome);
    }


    [Serializable]
    public abstract class TileBase : ITile
    {
        [XmlIgnore]
        public abstract string Name { get; set; }
        [XmlIgnore]
        public abstract char Code { get; set; }

        ITileVariant[] ITile.Variants => Variants;
        protected abstract ITileVariant[] Variants { get; set; }


        public ITileVariant GetVariant(int variant)
        {
            if (variant - 1 <= Variants.Length)
                return Variants[variant];
            return Variants[0];
        }

        public ITileVariant GetVariant(BiomeBase biome)
        {
            return GetVariant(biome.GetTileVariant(Name));
        }
    }
}
