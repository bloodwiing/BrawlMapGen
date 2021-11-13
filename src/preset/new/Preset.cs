using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace BMG.Preset.New
{
    public class Preset : PresetBase
    {
        // LOADING

        public static Preset LoadPreset(Meta meta)
        {
            if (meta.Format != PresetType.New)
                throw new ApplicationException("PRESET TYPE loading mismatch");


            Preset instance = new Preset();


            instance.tiles = LoadXMLArray<TilesRoot, Tile>(meta.TilesFile);


            var b = instance.tiles[0];
            var c = b.GetVariant(0).Layers[0];


            return instance;  // TODO RESUME TileVariant
        }

        private static A[] LoadXMLArray<T, A>(string file)
            where T : IArrayRoot<A>
        {
            using (StreamReader stream = new StreamReader(file))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                T root = (T)serializer.Deserialize(stream);
                root.SystemPath = Path.GetDirectoryName(file);
                return root.Array.ToArray();
            }
        }

        
        public TileBase[] tiles;


        public override BMG.TileBase[] Tiles => throw new System.NotImplementedException();
        public override Dictionary<string, BiomeBase> Biomes => throw new System.NotImplementedException();
        public override BiomeBase[] BiomeArray => throw new System.NotImplementedException();
        public override BiomeBase DefaultBiome => throw new System.NotImplementedException();
        public override Dictionary<string, GameModeDefinitionBase> GameModes => throw new System.NotImplementedException();


        public interface IArrayRoot<T>
        {
            string SystemPath { get; set; }
            string[] External { get; set; }
            List<T> Array { get; set; }
        }


        [Serializable, XmlRoot("Tiles")]
        public abstract class ArrayRootBase<T> : IArrayRoot<T>
        {
            [XmlIgnore]
            public string SystemPath { get; set; }

            [XmlElement("External")]
            public string[] External { get; set; }

            private bool loadedExternal = false;

            private void LoadFolder(string folder, string fileKey, string extensionKey)
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    string extension = Path.GetExtension(file);

                    if (extensionKey != ".*" && extension != extensionKey)
                        continue;

                    string name = Path.GetFileNameWithoutExtension(file);

                    if (fileKey != "*" && fileKey != "**" & name != file)
                        continue;

                    using (StreamReader stream = new StreamReader(file))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(T));
                        Array.Add((T)serializer.Deserialize(stream));
                    }
                }

                if (fileKey == "**")
                    foreach (string child in Directory.GetDirectories(folder))
                        LoadFolder(child, fileKey, extensionKey);
            }

            [XmlIgnore]
            public abstract List<T> Array { get; set; }
            List<T> IArrayRoot<T>.Array
            {
                get
                {
                    if (!loadedExternal)
                    {
                        foreach (string ext in External)
                        {
                            LoadFolder(
                                Path.Combine(SystemPath, Path.GetDirectoryName(ext)),  // Folder to scan
                                Path.GetFileNameWithoutExtension(ext),  // File name [can be wildcards * and **]
                                Path.GetExtension(ext)  // Extension [can be wildcard *]
                                );
                        }
                        loadedExternal = true;
                    }
                    return Array;
                }
                set => Array = value;
            }
        }


        [Serializable, XmlRoot("Tiles")]
        public class TilesRoot : ArrayRootBase<Tile>
        {
            [XmlElement("Tile")]
            public override List<Tile> Array { get; set; }
        }


        [Serializable, XmlRoot("Tile")]
        public class Tile : TileBase
        {
            [XmlAttribute("name")]
            public override string Name { get; set; }

            
            [XmlAttribute("code")]
            public string codeString
            {
                get => Code.ToString();
                set => Code = value.ToCharArray()[0];
            }
            public override char Code { get; set; }


            [XmlElement("Variant")]
            public TileVariant[] variants;
            protected override ITileVariant[] Variants
            {
                get => variants;
                set { }
            }
        }


        [Serializable]
        public class Offset : IOffset
        {
            [XmlAttribute("precision")]
            public int Precision { get; set; } = 1;

            [XmlIgnore]
            public Vector2 Point { get; set; } = new Vector2();

            [XmlElement("x")]
            public int X
            {
                get => Point.x;
                set => Point.x = value;
            }

            [XmlElement("y")]
            public int Y
            {
                get => Point.y;
                set => Point.y = value;
            }
        }


        [Serializable]
        public class TileAsset : ITileAsset
        {
            [XmlAttribute("asset")]
            public string Asset { get; set; }

            IOffset ITileAsset.Offset => Offset;
            public Offset Offset { get; set; } = new Offset();

            IEffect[] ITileAsset.Effects => Effects;
            [XmlArray("Effects")]
            [XmlArrayItem("Add", Type = typeof(AddEffect))]
            [XmlArrayItem("Multiply", Type = typeof(MultiplyEffect))]
            public EffectBase[] Effects = new EffectBase[0];
        }


        public class AddEffect : AddEffectBase
        {
            [XmlAttribute("color")]
            public override string Color { get; set; }
        }


        public class MultiplyEffect : MultiplyEffectBase
        {
            [XmlAttribute("color")]
            public override string Color { get; set; }
        }


        [Serializable]
        public class TileLayer : TileAsset, ITileLayer
        {
            [XmlAttribute("hIndex")]
            public int HIndex { get; set; } = 0;
            [XmlAttribute("zIndex")]
            public int ZIndex { get; set; } = 0;
        }


        [Serializable]
        public class TileVariant : ITileVariant
        {
            ITileLayer[] ITileVariant.Layers => Layers;
            [XmlElement("Layer")]
            public TileLayer[] Layers = new TileLayer[0];
        }
    }
}
