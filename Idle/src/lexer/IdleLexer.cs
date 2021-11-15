using System.Collections.Generic;

namespace Idle.Lexer
{
    public class IdleLexer
    {
        private static TokenDefinition[] m_definitions =
        {
            new TokenDefinition(TokenType.EOL, @"^\r?\n"),

            new TokenDefinition(TokenType.FRACTION, @"-?\d*\\.\d*"),
            new TokenDefinition(TokenType.NUMBER, @"^(-?\d+)"),

            new TokenDefinition(TokenType.TEXT_BLOCK, @"^(['""]){3}([\S\s]*)\1{3}"),
            new TokenDefinition(TokenType.TEXT_ROW, @"^(['""])([^\1\\]*?(?:\\.[^\1\\]*?)*)\1"),
            new TokenDefinition(TokenType.TEXT, @"^([A-Za-z_][\w-]*)"),

            new TokenDefinition(TokenType.BRACKET_L, @"^{"),
            new TokenDefinition(TokenType.BRACKET_R, @"^}"),

            new TokenDefinition(TokenType.EQUALS, @"^="),
            new TokenDefinition(TokenType.NOT, @"^!")
        };

        private string m_remaining;

        public IdleLexer(string input)
        {
            m_remaining = input;
        }

        public IEnumerator<Token> Tokenize()
        {
            while (!string.IsNullOrWhiteSpace(m_remaining))
            {
                var match = FindMatch(m_remaining);
                if (match.Matched)
                {
                    m_remaining = match.RemainingText;
                    yield return new Token(match);
                }
                else
                {
                    m_remaining = m_remaining.Substring(1);
                }
            }
        }

        private TokenMatch FindMatch(string input)
        {
            foreach (var def in m_definitions)
            {
                var match = def.Match(input);
                if (match.Matched)
                    return match;
            }

            return new TokenMatch() { Matched = false };
        }
    }
}
