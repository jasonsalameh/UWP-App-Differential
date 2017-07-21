using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows_App_Shared_Data
{
    public class AppxBlockMapBlock
    {
        public AppxBlockMapFile ParentFile { get; set; }
        public string Hash { get; set; }
        public uint CompressedSize { get; set; }
        public AppxBlockMapBlock()
        {
            Hash = string.Empty;
            CompressedSize = 0;
        }
    }
}
