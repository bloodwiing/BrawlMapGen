using System;
using Idle.Serialization;

namespace Idle
{
    [Serializable]
    public class test
    {
        [IdleProperty("COLOR")]
        public int[] Color;
    }
}
