using System.Text.RegularExpressions;

namespace Idle.Lexer
{
    class TokenDefinition
    {
        private readonly Regex m_pattern;
        private readonly TokenType m_type;

        public TokenDefinition(TokenType returnToken, string pattern)
        {
            m_pattern = new Regex(pattern, RegexOptions.IgnoreCase);
            m_type = returnToken;
        }

        public TokenMatch Match(string inputString)
        {
            var match = m_pattern.Match(inputString);

            if (match.Success)
            {
                string remainingText = string.Empty;
                if (match.Length != inputString.Length)
                    remainingText = inputString.Substring(match.Length);

                return new TokenMatch()
                {
                    Matched = true,
                    Type = m_type,
                    Value = match.Groups.Count > 1 ? match.Groups[match.Groups.Count - 1].Value : string.Empty,
                    RemainingText = remainingText
                };
            }
            else
            {
                return new TokenMatch() { Matched = false };
            }

        }
    }
}
