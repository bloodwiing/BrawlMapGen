using System;
using System.Xml.Serialization;

namespace BMG.Preset
{
    [Serializable]
    [XmlRoot("Preset")]
    public abstract class MetaBase
    {
        public string Name { get; set; }
        [XmlAttribute("displayName")]
        public string DisplayName { get; set; }

        public string GetDisplayName() => DisplayName != null ? DisplayName : Name;

        [XmlAttribute("version")]
        public string Version { get; set; }

        public virtual PresetType Format => PresetType.Invalid;

        public abstract PresetBase GetPreset();
    }
}
