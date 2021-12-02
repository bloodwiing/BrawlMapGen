using BMG.Abstract;
using Idle.Serialization;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace BMG.Preset.New
{
    [Serializable]
    public class GamesRoot : ArrayRootBase<GameArray, Game>
    {
        [IdleProperty("GAME")]
        public override List<Game> Array { get; protected set; } = new List<Game>();
    }


    [Serializable]
    public class GameArray : ITypeArray<Game>
    {
        [IdleProperty("GAME")]
        public Game[] Data { get; set; } = new Game[0];
    }


    [Serializable]
    public class Game
    {
        [IdleFlag("NAME")]
        public string Name;

        [IdleProperty("BASE")]
        private GameParams Base = new GameParams();

        [IdleProperty("CUSTOM")]
        private readonly Dictionary<string, GameParams> Customs = new Dictionary<string, GameParams>();

        public GameParams Fetch(IBiome biome)
        {
            if (Customs.TryGetValue(biome.Name, out GameParams game))
                return game;

            return Base;
        }
    }

    [Serializable]
    public class GameParams : IGame
    {
        [IdleProperty("INSERT")]
        public Insert[] Inserts;

        [IdleProperty("OVERRIDE")]
        public Dictionary<string, int> Override;

        [IdleProperty("MODIFY")]
        public Modify[] Modifies;

        public bool HasSpecialTiles => throw new NotImplementedException();

        public IEnumerator<GameGraphic> SpecialTiles => throw new NotImplementedException();

        public void ApplyOverrides(IBiome biome)
        {
            throw new NotImplementedException();
        }

        public void ApplyToState(IPreset preset)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class Insert
    {
        [IdleShortChildFlag("TILE")]
        public string Tile;

        [IdleProperty("TILE")]
        public int TileVariant;

        [IdleProperty("POS")]
        public IdlePoint Position;

        [IdleProperty("PASS")]
        public GameModePass Pass;
    }

    [Serializable]
    public class Modify
    {
        [IdleShortChildFlag("TILE")]
        public string Tile;

        [IdleProperty("POS")]
        public IdlePoint Position;
    }
}
