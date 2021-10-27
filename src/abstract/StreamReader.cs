using System.Text;

namespace BMG
{
    public class StreamReader : System.IO.StreamReader
    {
        public StreamReader(string path)
            : base(path)
            => Announce(path);

        public StreamReader(string path, bool detectEncodingFromByteOrderMarks)
            : base(path, detectEncodingFromByteOrderMarks)
            => Announce(path);

        public StreamReader(string path, Encoding encoding)
            : base(path, encoding)
            => Announce(path);

        public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
            : base(path, encoding, detectEncodingFromByteOrderMarks)
            => Announce(path);

        public StreamReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
            : base(path, encoding, detectEncodingFromByteOrderMarks, bufferSize)
            => Announce(path);


        private void Announce(string path) => Logger.LogAAL(Logger.AALDirection.In, path);
    }
}
