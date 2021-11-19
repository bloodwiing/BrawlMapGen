using Idle.Lexer;
using System;

namespace Idle.Parser
{
    public struct Data
    {
        public object value;
        public PropertyType type;

        public Data(Data data)
        {
            value = data.value;
            type = data.type;
        }

        public Data(string value, PropertyType propertyType)
        {
            this.value = value;
            type = propertyType;
        }

        public Data(string value, TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.TEXT_BLOCK:
                case TokenType.TEXT_ROW:
                case TokenType.TEXT:
                    type = PropertyType.TEXT;
                    this.value = value;
                    break;

                case TokenType.NUMBER:
                    type = PropertyType.NUMBER;
                    this.value = int.Parse(value);
                    break;

                case TokenType.FRACTION:
                    type = PropertyType.FRACTION;
                    this.value = float.Parse(value);
                    break;

                case TokenType.MACRO:
                    
                    switch (value)
                    {
                        case "NULL":
                            type = PropertyType.NULL;
                            this.value = null;
                            break;

                        case "TRUE":
                        case "FALSE":
                            type = PropertyType.BOOLEAN;
                            this.value = value == "TRUE";
                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public Data(Token token) : this(token.Data, token.Type) { }

        public Data(Atom atom)
        {
            value = atom;
            type = PropertyType.ATOM;
        }
    }
}
