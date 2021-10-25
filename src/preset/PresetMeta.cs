using System.Collections.Generic;

namespace BMG
{
    public class PresetMeta
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public PresetType Format { get; set; } = PresetType.Invalid;
        public Dictionary<string, object> Options { get; set; }
    }
}
