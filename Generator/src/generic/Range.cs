using System.Collections;
using System.Collections.Generic;

namespace BMG
{
    public struct Range : IEnumerable<int>
    {
        int low, high;


        public Range(int value)
        {
            low = high = value;
        }

        public Range(int low, int high)
        {
            this.low = low;
            this.high = high;
        }


        public static implicit operator (int, int)(Range self)
        {
            return (self.low, self.high);
        }


        public IEnumerator<int> GetEnumerator()
        {
            for (int i = low; i <= high; i++)
                yield return i;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return GetEnumerator();
        }


        public void Insert(int value)
        {
            if (value < low)
                low = value;
            if (value > high)
                high = value;
        }
    }
}
