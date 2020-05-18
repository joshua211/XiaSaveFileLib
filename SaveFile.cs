using Newtonsoft.Json.Linq;
using System;
using XiaWorld;

namespace XiaSaveFileLib
{
    public class SaveFile
    {
        public FileHeader Header { get; set; }
        public string FileContent { get; set; }

        public class FileHeader
        {
            public float Version { get; set; }
            public string WriteTime { get; set; }
            public g_emGameMode Mode { get; set; }
            public bool Decompressed { get; set; }
            public string FileName { get; set; }
            public string[] Mods { get; set; }
            public FileHeader() { }

        }
    }

}