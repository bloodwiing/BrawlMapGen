using System.Drawing;

namespace BMG
{
    public interface IBiome
    {
        string Name { get; }

        void ResetOverrides();
        void ApplyOverride(string tile, int type);

        int GetTileVariant(ITile tile);

        bool HasBackground { get; }

        Color SolveBackgroundColor();
    }
}
