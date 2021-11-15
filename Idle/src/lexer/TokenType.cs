namespace Idle
{
    public enum TokenType
    {
        EOF,
        EOL,  // \r\n

        TEXT_BLOCK,  // """ A """
        TEXT_ROW,  // "A B C"
        TEXT,  // ABC
        
        NUMBER,  // 123
        FRACTION,  // 1.2

        BRACKET_L,  // {
        BRACKET_R,  // }

        EQUALS,  // =
        NOT  // !
    }
}
