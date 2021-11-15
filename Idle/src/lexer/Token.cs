namespace Idle.Lexer
{
    public class Token
    {
        public TokenType Type { get; }
        public string Data { get; }

        public Token()
        {
            Type = TokenType.EOF;
            Data = string.Empty;
        }

        public Token(TokenMatch match)
        {
            Type = match.Type;
            Data = match.Value;
        }

        public override string ToString()
        {
            if (Data == string.Empty)
                return Type.ToString();
            return $"{Type}:{Data}";
        }
    }
}
