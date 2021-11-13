using System;
using System.IO;
using System.Xml.Serialization;

namespace BMG.Preset
{
    [Serializable]
    [XmlRoot("Preset")]
    public abstract class MetaBase
    {
        public string SystemName { get; set; }
        [XmlAttribute("displayName")]
        public string DisplayName { get; set; }

        public string GetDisplayName() => DisplayName != null ? DisplayName : SystemName;

        [XmlAttribute("version")]
        public string Version { get; set; }

        public virtual PresetType Format => PresetType.Invalid;

        public abstract PresetBase GetPreset();

        public string SystemPath => Path.Combine(".", "presets", SystemName);
    }
}
