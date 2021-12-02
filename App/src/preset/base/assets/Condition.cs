namespace BMG
{
    public class Condition : Trinary
    {
        public Condition(string text) : base(text) { }

        public static implicit operator Condition(string input)
        {
            return new Condition(input);
        }

        public bool Check(byte compare)
        {
            return unchecked((byte)~(constant ^ compare) | wild) == 255;
        }
    }
}
