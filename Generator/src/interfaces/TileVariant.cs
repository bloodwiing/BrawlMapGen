using System;
using System.Xml.Serialization;

namespace BMG.Preset
{
    public interface IEffect
    {
        EffectType Type { get; }
    }


    public abstract class EffectBase : IEffect
    {
        public virtual EffectType Type => EffectType.Invalid;
    }


    [Serializable]
    public abstract class AddEffectBase : EffectBase
    {
        public override EffectType Type => EffectType.Add;

        [XmlIgnore]
        public abstract string Color { get; set; }
    }


    [Serializable]
    public abstract class MultiplyEffectBase : EffectBase
    {
        public override EffectType Type => EffectType.Multiply;

        [XmlIgnore]
        public abstract string Color { get; set; }
    }


    public interface IOffset
    {
        int Precision { get; }
        Vector2 Point { get; }
    }


    public interface ITileAsset
    {
        string Asset { get; }
        IOffset Offset { get; }
        IEffect[] Effects { get; }
    }


    public interface ITileLayer : ITileAsset
    {
        int HIndex { get; }
        int ZIndex { get; }
    }

    
    public interface ITileVariant
    {
        ITileLayer[] Layers { get; }
    }
}
