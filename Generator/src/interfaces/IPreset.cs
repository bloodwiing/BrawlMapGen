namespace BMG
{
    public interface IPreset
    {
        ITile GetTile(string name);
        ITile GetTile(char code);

        bool MakeTileGraphic(IBiome biome, ITile tile, out Graphic graphic);
        bool MakeTileGraphic(ITile tile, int variant, out Graphic graphic);


        // BIOMES

        IBiome DefaultBiome { get; }

        IBiome GetBiome(int index);
        IBiome GetBiome(string name);

        IBiome GetBiome(IMap map);


        // GAME MODES

        IGame GetGame(string name, IBiome biome);


        // EXTRA

        Range GetIndexRange(IMap map);
    }
}
