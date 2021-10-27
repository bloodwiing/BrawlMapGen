using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace BMG.Preset
{
    public class Loader
    {
        public static PresetBase LoadPreset(string name)
        {
            string file = Path.Combine(".", "presets", name, "Preset.xml");


            if (!File.Exists(file))
                throw new ApplicationException($"PRESET doesn't exist\n  [FileReader] Unable to find file in location '{file}'");


            MetaBase meta;


            switch (ProcessFile(file, out XmlDocument document))
            {
                case PresetType.Old:
                    meta = Deserialize<Old.Meta>(document);
                    break;

                case PresetType.New:
                    meta = Deserialize<New.Meta>(document);
                    break;

                default:
                    throw new ApplicationException("PRESET of unsupported FORMAT!");
            }


            meta.Name = name;


            return meta.GetPreset();
        }


        private static PresetType ProcessFile(string file, out XmlDocument document)
        {
            document = new XmlDocument();


            Logger.LogAAL(Logger.AALDirection.In, file);
            document.Load(file);


            XmlProcessingInstruction format =
                document
                .ChildNodes
                .OfType<XmlProcessingInstruction>()
                .Where(x => x.Name == "format")
                .FirstOrDefault();


            if (!int.TryParse(format.Value, out int result))
                throw new ApplicationException("Format needs to be an INT");


            return (PresetType)result;
        }

        private static T Deserialize<T>(XmlDocument document)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlNodeReader reader = new XmlNodeReader(document);

            return (T)serializer.Deserialize(reader);
        }


        public bool TryLoadPreset(string name, out PresetBase preset)
        {
            try
            {
                preset = LoadPreset(name);
                return true;
            }
            catch
            {
                preset = null;
                return false;
            }
        }
    }
}
