namespace BMG
{
    public abstract class TileBase
    {
        public abstract string Name { get; }
        public abstract char Code { get; }

        protected abstract TileVariantBase[] Variants { get; }


        public TileVariantBase GetVariant(int variant)
        {
            if (variant - 1 <= Variants.Length)
                return Variants[variant];
            return Variants[0];
        }

        public TileVariantBase GetVariant(BiomeBase biome)
        {
            return GetVariant(biome.GetTileVariant(Name));
        }
    }
}
