using System;
using System.Drawing;

namespace BMG
{
    public struct Vector2
    {
        public int x;
        public int y;

        public int precision;

        public Vector2(int x, int y)
        {
            this.x = x;
            this.y = y;

            precision = 1;
        }
        public Vector2(int x, int y, int p)
        {
            this.x = x;
            this.y = y;

            precision = p;
        }

        public Vector2 Clone()
        {
            return new Vector2(x, y, precision);
        }


        public void SetX(int x) => this.x = x;
        public void SetY(int y) => this.y = y;
        public void SetPrecision(int p) => precision = p;


        public static implicit operator Vector2(int value)
        {
            return new Vector2(value, value);
        }

        public static implicit operator Vector2((float, float) value)
        {
            return new Vector2((int)MathF.Round(value.Item1), (int)MathF.Round(value.Item2));
        }

        public static implicit operator Point(Vector2 vector)
        {
            return new Point(vector.x, vector.y);
        }


        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(
                a.x + b.x,
                a.y + b.y
                );
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(
                a.x - b.x,
                a.y - b.y
                );
        }

        public static Vector2 operator *(Vector2 a, Vector2 b)
        {
            return new Vector2(
                a.x * b.x,
                a.y * b.y
                );
        }

        public static Vector2 operator /(Vector2 a, Vector2 b)
        {
            return new Vector2(
                a.x / b.x,
                a.y / b.y
                );
        }

        public static Vector2 Zero => new Vector2(0, 0);
    }
}
