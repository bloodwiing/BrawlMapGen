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
            // MAKE PATH

            string file = Path.Combine(".", "presets", name, "Preset.xml");


            // CHECK FILE EXISTENCE

            if (!File.Exists(file))
                throw new ApplicationException($"PRESET doesn't exist\n  [FileReader] Unable to find file in location '{file}'");


            // PROCESS AND DESERIALIZE

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


            // INSERT PRESET SYSTEM NAME

            meta.SystemName = name;


            // RETURN

            return meta.GetPreset();
        }


        private static PresetType ProcessFile(string file, out XmlDocument document)
        {
            // PREPARE

            document = new XmlDocument();


            // LOAD PRESET FILE

            Logger.LogAAL(Logger.AALDirection.In, file);
            document.Load(file);


            // LOOK FOR AND GET <?format ?>

            XmlProcessingInstruction format =
                document
                .ChildNodes
                .OfType<XmlProcessingInstruction>()
                .Where(x => x.Name == "format")
                .FirstOrDefault();


            // PARSE INT IF POSSIBLE

            if (!int.TryParse(format.Value, out int result))
                throw new ApplicationException("Format needs to be an INT");


            // CONVERT TO ENUM

            return (PresetType)result;
        }

        private static T Deserialize<T>(XmlDocument document)
        {
            // PREPARE DESERIALIZATION

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlNodeReader reader = new XmlNodeReader(document);


            // DESERIALIZE AND RETURN

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
