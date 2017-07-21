using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows_App_Shared_Data
{
    public class AppxBlockMapFile
    {
        public string FileName { get; set; }
        public ulong UncompressedSize { get; set; }
        public uint LocalFileHeaderSize { get; set; }
        public List<AppxBlockMapBlock> Blocks { get; set; }
        public string FinalFileHash { get; set; }
        public AppxPackage ParentPackage { get; set; }
        public AppxBlockMapFile()
        {
            FileName = string.Empty;
            UncompressedSize = 0;
            LocalFileHeaderSize = 0;
            Blocks = new List<AppxBlockMapBlock>();
            FinalFileHash = string.Empty;
            ParentPackage = null;

        }
    }
}
