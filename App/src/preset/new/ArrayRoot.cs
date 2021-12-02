using Idle;
using Idle.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace BMG.Preset.New
{
    public interface IArrayRoot<T>
    {
        string SystemPath { get; set; }
        string[] External { get; set; }
        List<T> Array { get; set; }
    }

    public interface ITypeArray<T>
    {
        T[] Data { get; set; }
    }

    [Serializable]
    public abstract class ArrayRootBase<A, T> : IArrayRoot<T>
        where A : ITypeArray<T>
    {
        public string SystemPath { get; set; }

        [IdleProperty("EXTERNAL")]
        public string[] External { get; set; }

        //private bool loadedExternal { get => false; set => throw new Exception(); }
        private bool loadedExternal = false;

        private void LoadFolder(string folder, string fileKey, string extensionKey)
        {
            foreach (string file in Directory.GetFiles(folder))
            {
                string extension = Path.GetExtension(file);

                if (extensionKey != ".*" && extension != extensionKey)
                    continue;

                string name = Path.GetFileNameWithoutExtension(file);

                if (fileKey != "*" && fileKey != "**" && name != file)
                    continue;

                IdleReader reader = new IdleReader(file);
                var c = IdleSerializer.Deserialize<A>(reader);
                Array.AddRange(c.Data);
            }

            if (fileKey == "**")
                foreach (string child in Directory.GetDirectories(folder))
                    LoadFolder(child, fileKey, extensionKey);
        }

        public abstract List<T> Array { get; protected set; }
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
}
