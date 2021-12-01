using System.IO;
using System.Xml.Serialization;

namespace BMG.Preset.New
{
    [XmlRoot("Preset")]
    public class Meta : MetaBase
    {
        public override PresetType Format => PresetType.New;


        [XmlElement("Linker")]
        public Linker Linker { get; set; }


        public override IPreset GetPreset()
        {
            return Preset.LoadPreset(this);
        }


        public string BiomesFile => Path.Combine(SystemPath, Linker.Biomes);
        public string GamesFile => Path.Combine(SystemPath, Linker.Games);
        public string TilesFile => Path.Combine(SystemPath, Linker.Tiles);
    }


    public class Linker
    {
        public string Biomes { get; set; }
        public string Games { get; set; }
        public string Tiles { get; set; }
    }
}
