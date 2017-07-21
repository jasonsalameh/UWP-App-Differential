using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows_App_Shared_Data
{
    public class AppxPackageComparisonResult
    {
        // Files
        public Dictionary<AppxPackage, List<List<AppxBlockMapFile>>> InterPackageDuplicateFiles { get; set; }
        public Dictionary<string, List<AppxBlockMapFile>> CrossPackageDeDupdFiles { get; set; }
        public ulong OptimizedSizeOfApps { get; set; }
        public ulong UnoptimizedSizeOfApps { get; set; }

        // Updates
        public ulong BytesHardlinked { get; set; }
        public ulong BlocksDownloaded { get; set; }
        public ulong BlocksDeleted { get; set; }
        public ulong BlocksCopied { get; set; }
        public Dictionary<string, ChangeResult> FileHardLinkChangeResults { get; set; }
        public Dictionary<string, ChangeResult> BlockDownloadChangeResults { get; set; }
        public Dictionary<string, ChangeResult> BlockCopiedChangeResults { get; set; }
        public Dictionary<string, ChangeResult> BlockDeletedChangeResults { get; set; }

        public AppxPackageComparisonResult()
        {
            InterPackageDuplicateFiles = new Dictionary<AppxPackage, List<List<AppxBlockMapFile>>>();
            CrossPackageDeDupdFiles = new Dictionary<string, List<AppxBlockMapFile>>();
            FileHardLinkChangeResults = new Dictionary<string, ChangeResult>();
            BlockDownloadChangeResults = new Dictionary<string, ChangeResult>();
            BlockCopiedChangeResults = new Dictionary<string, ChangeResult>();
            BlockDeletedChangeResults = new Dictionary<string, ChangeResult>();

            OptimizedSizeOfApps = 0;
            UnoptimizedSizeOfApps = 0;
            BytesHardlinked = 0;
            BlocksDownloaded = 0;
            BlocksDeleted = 0;
            BlocksCopied = 0;
        }
    }

    public class ChangeResult
    {
        public string Package { get; set; }
        public string FileName { get; set; }
        public string BlockHash { get; set; }
        public ulong Size { get; set; }
    }
}
