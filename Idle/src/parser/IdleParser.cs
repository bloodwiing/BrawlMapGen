using System;
using System.Collections.Generic;
using System.Text;
using Idle.Lexer;

namespace Idle.Parser
{
    public class IdleParser
    {
        private readonly Atom m_head;
        private readonly IEnumerator<Token> m_enum;

        public IdleParser(IEnumerator<Token> tokens)
        {
            m_head = new Atom(null, null);
            m_enum = tokens;
        }

        public Atom Parse()
        {
            PopulateAtom(m_head);
            return m_head;
        }

        private void PopulateAtom(Atom atom)
        {
            while (m_enum.MoveNext())
            {
                switch (m_enum.Current.Type)
                {
                    // If new property is } or EOL, end atom
                    case TokenType.EOF:
                    case TokenType.BRACKET_R:
                        return;

                    // If empty line, no property
                    case TokenType.EOS:
                        continue;

                    default:
                        if (ReadProperty(atom))
                            return;
                        break;
                }
            }
        }

        private bool ReadProperty(Atom atom)
        {
            bool interruped = false;

            if (m_enum.Current.Type != TokenType.TEXT)
                throw new Exception("Property labels must be TEXT");

            Property property = atom.CreateOrGetProperty(m_enum.Current.Data);
            Item pItem = property.NewItem();
            Data? previous = null;

            bool nextFlagNegative = false;

            while (m_enum.MoveNext())
            {
                if (m_enum.Current.Type == TokenType.BRACKET_R)
                {
                    interruped = true;
                    break;
                }

                // New line - end property
                if (m_enum.Current.Type == TokenType.EOS)
                    break;

                // If it's a value flag, read further and set it
                if (m_enum.Current.Type == TokenType.EQUALS)
                {
                    m_enum.MoveNext();

                    switch (m_enum.Current.Type)
                    {
                        case TokenType.TEXT_ROW:
                        case TokenType.TEXT:
                        case TokenType.FRACTION:
                        case TokenType.NUMBER:
                        case TokenType.MACRO:

                            pItem.SetFlag(new Flag()
                            {
                                Name = (string)previous.Value.value,
                                Value = new Data(m_enum.Current),
                                Negated = nextFlagNegative
                            });
                            nextFlagNegative = false;
                            previous = null;

                            continue;

                        default:

                            throw new Exception("Value Flags must have TEXT_ROW, TEXT, NUMBER or FRACTION as the Value");
                    }
                }

                // If there was a label previously, it is a flag
                if (previous != null)
                    pItem.SetFlag(previous, nextFlagNegative);

                // If { then it means an atom is the value
                if (m_enum.Current.Type == TokenType.BRACKET_L)
                {
                    // Instantiate child
                    Atom child = new Atom(atom, property);

                    // Reference own flags
                    child.PassFlags(pItem.InternalFlags);

                    // Fill properties
                    PopulateAtom(child);

                    // Set as value for property item
                    pItem.SetValue(new Data(child));

                    // Return as if uninterrupted
                    return false;
                }

                if (m_enum.Current.Type == TokenType.NOT)
                {
                    nextFlagNegative = true;
                    continue;
                }

                // New last label
                previous = new Data(m_enum.Current);
            }

            if (previous == null)
                throw new Exception($"Property '{property.Label}' must have a Value");

            pItem.SetValue(previous.Value);

            return interruped;
        }
    }
}
