using System.IO;
using Idle.Lexer;
using Idle.Parser;

namespace Idle
{
    public class IdleReader
    {
        public Atom HeadAtom { get; private set; }

        public IdleReader(TextReader reader)
        {
            Read(reader);
        }

        public IdleReader(string path)
        {
            using (StreamReader stream = new StreamReader(path))
            {
                Read(stream);
            }
        }

        private void Read(TextReader reader)
        {
            string data = reader.ReadToEnd();

            IdleLexer lexer = new IdleLexer(data);
            var tokens = lexer.Tokenize();

            IdleParser parser = new IdleParser(tokens);
            HeadAtom = parser.Parse();

            tokens.Dispose();
        }
    }
}
