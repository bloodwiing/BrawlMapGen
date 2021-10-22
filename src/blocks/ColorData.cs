using AMGBlocks.Value;
using System;

namespace AMGBlocks
{
    
    public class ColorData
    {
        public byte r { get; set; }
        public byte g { get; set; }
        public byte b { get; set; }

        public static byte ByteClamp(int value)
        {
            if (value > 255)
                return (byte)(value % 256);
            if (value < 0)
                return 0;
            return (byte)value;
        }
        public static byte ByteClamp(float value) => ByteClamp((int)MathF.Round(value));

        public static implicit operator ColorData(int value)
        {
            byte newValue = ByteClamp(value);
            return new ColorData()
            {
                r = newValue,
                g = newValue,
                b = newValue
            };
        }

        public override bool Equals(object obj)
        {
            return obj is ColorData data &&
                   r == data.r &&
                   g == data.g &&
                   b == data.b;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(r, g, b);
        }

        public static ColorData operator +(ColorData a, ColorData b)
        {
            return new ColorData()
            {
                r = ByteClamp(a.r + b.r),
                g = ByteClamp(a.g + b.g),
                b = ByteClamp(a.b + b.b)
            };
        }

        public static ColorData operator -(ColorData a, ColorData b)
        {
            return new ColorData()
            {
                r = ByteClamp(a.r - b.r),
                g = ByteClamp(a.g - b.g),
                b = ByteClamp(a.b - b.b)
            };
        }

        public static ColorData operator *(ColorData a, ColorData b)
        {
            return new ColorData()
            {
                r = ByteClamp(a.r * b.r / 255),
                g = ByteClamp(a.g * b.g / 255),
                b = ByteClamp(a.b * b.b / 255)
            };
        }

        public static ColorData operator /(ColorData a, ColorData b)
        {
            return new ColorData()
            {
                r = ByteClamp(b.r * 255 / a.r),
                g = ByteClamp(b.g * 255 / a.g),
                b = ByteClamp(b.b * 255 / a.b)
            };
        }

        public static ColorData operator %(ColorData a, ColorData b)
        {
            return new ColorData()
            {
                r = ByteClamp(b.r * 255 % a.r),
                g = ByteClamp(b.g * 255 % a.g),
                b = ByteClamp(b.b * 255 % a.b)
            };
        }

        public static ColorData Pow(ColorData b, float p)
        {
            return new ColorData()
            {
                r = ByteClamp(MathF.Pow((float)b.r / 255, p)),
                g = ByteClamp(MathF.Pow((float)b.g / 255, p)),
                b = ByteClamp(MathF.Pow((float)b.b / 255, p))
            };
        }
        public ColorData Pow(float p) => Pow(this, p);

        public static bool operator ==(ColorData a, ColorData b)
        {
            return (a.r == b.r) && (a.g == b.g) && (a.b == b.b);
        }

        public static bool operator !=(ColorData a, ColorData b)
        {
            return (a.r != b.r) || (a.g != b.g) || (a.b != b.b);
        }

        public static bool operator >(ColorData a, ColorData b)
        {
            return a.r + a.g + a.b > b.r + b.g + b.b;
        }

        public static bool operator >=(ColorData a, ColorData b)
        {
            return a.r + a.g + a.b >= b.r + b.g + b.b;
        }

        public static bool operator <(ColorData a, ColorData b)
        {
            return a.r + a.g + a.b < b.r + b.g + b.b;
        }

        public static bool operator <=(ColorData a, ColorData b)
        {
            return a.r + a.g + a.b <= b.r + b.g + b.b;
        }

        public override string ToString()
        {
            return string.Format("ColorData<R {0}, G {1}, B {2}>", r, g, b);
        }

        public static bool ObjectHasColorData(object obj)
        {
            return obj is ColorData || (obj is MathBlock math && math.IsColor) || (obj is PARAMETERBlock par && par.ValueType() == BlockData.ValueType.color);
        }
    }
}
