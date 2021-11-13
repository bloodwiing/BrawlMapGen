using BMG.State;
using System.Collections.Generic;

namespace BMG
{
    public abstract class GameModeBase
    {
        public abstract class SpecialBase
        {
            public abstract string Tile { get; }
            public abstract int Type { get; }
            public abstract string Position { get; }
            public abstract GameModePass Pass { get; }
        }


        public abstract class ModBase
        {
            public abstract string Tile { get; }
            public abstract string Position { get; }
        }


        public abstract SpecialBase[] SpecialTiles { get; }
        public abstract Dictionary<string, int> BiomeOverrides { get; }
        public abstract ModBase[] MapMods { get; }

        public void ApplyToState(Preset.PresetBase preset)
        {
            if (MapMods == null)
                return;

            foreach (ModBase mod in MapMods)
            {
                Vector2 position = Utils.ParsePosition(mod.Position);

                TileBase tile = preset.GetTile(mod.Tile);

                var row = AMGState.map.data[position.y].ToCharArray();
                row[position.x] = tile.Code;
                AMGState.map.data[position.y] = string.Join("", row);

                //Logger.LogTile(new TileActionTypes(1, 0, 1, 0, 0), oTile, ysLoc, xsLoc, yLength, xLength, Logger.TileEvent.gamemodeModding);
            }
        }

        public void ApplyOverrides(BiomeBase biome)
        {
            foreach ((string tile, int type) in BiomeOverrides)
                biome.ApplyOverride(tile, type);
        }
    }


    public abstract class GameModeDefinitionBase : GameModeBase
    {
        public abstract string Name { get; }

        public abstract Dictionary<string, GameModeBase> Variants { get; }
    }
}
