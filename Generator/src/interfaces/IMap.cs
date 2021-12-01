namespace BMG
{
    public interface IMap
    {
        //string RawName { get; }
        string GetName();

        bool IsEmpty { get; }
        //string[] Data { get; }
        //object Biome { get; }
        IBiome GetBiome(IPreset preset);

        Rectangle Size { get; }
        void AutoCrop(char[] tiles);

        int Scale { get; }

        void UseForAutoCrop(OptionsBase options);
        bool IsVoid(char c);

        void ApplyOverrides(IBiome biome);

        Margin Margin { get; }

        IGame GetGame(IPreset preset, IBiome biome);

        //int? GenerationSeed { get; }
    }
}
