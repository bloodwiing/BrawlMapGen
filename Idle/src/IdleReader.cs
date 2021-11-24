using System.IO;
using Idle.Lexer;
using Idle.Parser;

namespace Idle
{
    public class IdleReader
    {
        public readonly Atom HeadAtom;

        public IdleReader(string path)
        {
            string data;

            using (StreamReader stream = new StreamReader(path))
            {
                data = stream.ReadToEnd();
            }

            IdleLexer lexer = new IdleLexer(data);
            var tokens = lexer.Tokenize();

            IdleParser parser = new IdleParser(tokens);
            HeadAtom = parser.Parse();

            tokens.Dispose();
        }
    }
}
