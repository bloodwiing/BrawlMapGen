using System;

namespace BMG
{
    public class Trinary
    {
        public readonly byte constant;
        public readonly byte wild;

        public Trinary(string text)
        {
            byte con = 0, wild = 0;

            text = text.Replace(" ", "");

            foreach (char c in text)
            {
                con <<= 1;
                wild <<= 1;

                switch (c)
                {
                    case '1':
                        con++;
                        break;

                    case '0':
                        break;

                    case '-':
                        wild++;
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            constant = con;
            this.wild = wild;
        }

        public static implicit operator Trinary(string input)
        {
            return new Trinary(input);
        }
    }
}
