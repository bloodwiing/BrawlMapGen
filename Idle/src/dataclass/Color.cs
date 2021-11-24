using Idle.Exceptions;
using System;
using System.Text.RegularExpressions;

namespace Idle
{
    public struct Color
    {
        public byte r, g, b, a;

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public Color(Color color) : this(color.r, color.b, color.g, color.a) { }


        public static Color Zero => new Color(0, 0, 0);

        public static Color Red => new Color(255, 0, 0);
        public static Color Green => new Color(0, 255, 0);
        public static Color Blue => new Color(0, 0, 255);


        public static Color Parse(string input)
        {
            input = input.Trim().TrimStart('#');

            if (!Regex.IsMatch(input, @"[A-Z\d]+"))
                throw new ColorParseException();

            uint value = uint.Parse(input, System.Globalization.NumberStyles.HexNumber);

            switch (input.Length)
            {
                // RGB
                case 3:
                    return new Color(
                        Read4Bit(value, 2),
                        Read4Bit(value, 1),
                        Read4Bit(value, 0));

                // RGBA
                case 4:
                    return new Color(
                        Read4Bit(value, 3),
                        Read4Bit(value, 2),
                        Read4Bit(value, 1),
                        Read4Bit(value, 0));

                // RRGGBB
                case 6:
                    return new Color(
                        Read8Bit(value, 2),
                        Read8Bit(value, 1),
                        Read8Bit(value, 0));

                // RRGGBBAA
                case 8:
                    return new Color(
                        Read8Bit(value, 3),
                        Read8Bit(value, 2),
                        Read8Bit(value, 1),
                        Read8Bit(value, 0));

                default:
                    throw new NotImplementedException();
            }
        }

        public static bool TryParse(string input, out Color color)
        {
            try
            {
                color = Parse(input);
                return true;
            }
            catch
            {
                color = Zero;
                return false;
            }
        }


        private static byte Make8Bit(byte data)
        {
            return (byte)((data << 4) + data);
        }

        private static byte Read4Bit(uint data, int index)
        {
            return Make8Bit((byte)((data >> index * 4) & 15));
        }

        private static byte Read8Bit(uint data, int index)
        {
            return (byte)((data >> index * 8) & 255);
        }


        public override string ToString()
        {
            return $"#{r:x2}{g:x2}{b:x2}{a:x2}";
        }
    }
}
