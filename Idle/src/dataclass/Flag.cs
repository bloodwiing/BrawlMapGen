using Idle.Parser;
using System;

namespace Idle
{
    public class Flag
    {
        public string Name;
        public bool Negated;
        public Data Value = new Data(string.Empty, PropertyType.UNSET);

        public static implicit operator Flag(string input)
        {
            return new Flag() { Name = input };
        }

        public static implicit operator Flag(Data input)
        {
            if (input.type != PropertyType.TEXT)
                throw new Exception("Flag Name must be TEXT");
            return new Flag() { Name = (string)input.value };
        }
    }
}
