namespace BMG
{
    public abstract class EffectBase
    {
        public abstract EffectType type { get; }
    }


    public abstract class TileAssetBase
    {
        public abstract Vector2 Offset { get; set; }
        public abstract string Asset { get; }

        public abstract EffectBase[] Effects { get; }
    }


    public abstract class TileVariantBase : TileAssetBase
    {
        public abstract int RowLayer { get; }
        public abstract int Layer { get; }

        public abstract TileAssetBase[] Randomizer { get; }
    }
}
