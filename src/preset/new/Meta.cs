using System.Xml.Serialization;

namespace BMG.Preset.New
{
    [XmlRoot("Preset")]
    public class Meta : MetaBase
    {
        public override PresetType Format => PresetType.New;


        [XmlElement("Linker")]
        public Linker Linker { get; set; }


        public override PresetBase GetPreset()
        {
            throw new System.NotImplementedException();
        }
    }


    public class Linker
    {
        public string Biomes { get; set; }
        public string Modes { get; set; }
        public string Tiles { get; set; }
    }
}
