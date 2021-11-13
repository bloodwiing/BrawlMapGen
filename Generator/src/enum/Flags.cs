using System;

namespace BMG
{
    [Flags]
    public enum BMGEvent
    {
        SETUP = 1,
        DRAW = 1 << 1,
        EXPORT = 1 << 2,
        AAL = 1 << 3,
        STATUS = 1 << 4,
        MOD = 1 << 5
    }
}
