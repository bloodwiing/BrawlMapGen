using Idle.Serialization;
using System;
using System.Collections.Generic;

namespace BMG.Preset.New
{
    [Serializable]
    public class TilesRoot : ArrayRootBase<TileArray, Tile>
    {
        [IdleProperty("TILE")]
        public override List<Tile> Array { get; protected set; } = new List<Tile>();
    }


    [Serializable]
    public class TileArray : ITypeArray<Tile>
    {
        [IdleProperty("TILE")]
        public Tile[] Data { get; set; } = new Tile[0];
    }


    [Serializable]
    public class Tile : ITile
    {
        [IdleFlag("NAME")]
        public string Name { get; set; }


        [IdleFlag("CODE")]
        public string CodeString
        {
            get => Code.ToString();
            set => Code = value.ToCharArray()[0];
        }
        public char Code { get; private set; }


        [IdleProperty("VARIANT")]
        public TileVariant[] variants;
    }


    [Serializable]
    public class Offset
    {
        private readonly Vector2 Point = new Vector2();

        [IdleFlag("PRECISION")]
        public int Precision
        {
            get => Point.precision;
            set => Point.SetPrecision(value);
        }

        [IdleProperty("X")]
        public int X
        {
            get => Point.x;
            set => Point.SetX(value);
        }

        [IdleProperty("Y")]
        public int Y
        {
            get => Point.y;
            set => Point.SetY(value);
        }
    }


    public class AddEffect
    {
        [IdleProperty("EFFECT")]
        public string Color { get; set; }
    }


    public class MultiplyEffect
    {
        [IdleProperty("EFFECT")]
        public string Color { get; set; }
    }


    [Serializable]
    public class TileLayer
    {
        [IdleProperty("ASSET")]
        public string Asset;

        [IdleProperty("HINDEX")]
        public int HIndex = 0;
        [IdleProperty("ZINDEX")]
        public int ZIndex = 0;

        public Offset Offset;

        //[XmlArray("Effects")]
        //[XmlArrayItem("Add", Type = typeof(AddEffect))]
        //[XmlArrayItem("Multiply", Type = typeof(MultiplyEffect))]
        //public EffectBase[] Effects = new EffectBase[0];
    }


    [Serializable]
    public class TileVariant
    {
        [IdleProperty("LAYER")]
        public TileLayer[] Layers = new TileLayer[0];
    }
}
