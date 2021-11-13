using System.Xml.Serialization;

namespace BMG.Preset.Old
{
    [XmlRoot("Preset")]
    public class Meta : MetaBase
    {
        public override PresetType Format => PresetType.Old;


        [XmlElement("Linker")]
        public Linker Linker { get; set; }


        public override PresetBase GetPreset()
        {
            return Preset.LoadPreset(this);
        }
    }


    public class Linker
    {
        public string Data { get; set; }
        public string Assets { get; set; }
    }
}
