using BMG.Preset;
using Idle.Serialization;

namespace BMG
{
    public class AssetVariant
    {
        [IdleFlag("ID")]
        public string ID { get; protected set; }

        [IdleShortChildFlag("FILE")]
        public FileType FileType { get; protected set; }

        [IdleProperty("FILE")]
        public string FileName { get; protected set; }
    }
}
