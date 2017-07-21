using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows_App_Shared_Data
{
    public class AppxPackage
    {
        public enum PACKAGE_TYPE
        {
            PACKAGE_TYPE_APPX_PACKAGE,
            PACKAGE_TYPE_APPX_BUNDLE,
            PACKAGE_TYPE_APPX_RESOUCE
        }
        public Dictionary<string, List<AppxBlockMapFile>> BlockMap { get; set; }
        public AppxManifest Manifest { get; set; }
        public AppxPackage ParentPackage { get; set; }
        public PACKAGE_TYPE PackageType { get; set; }
        public ulong UncompressedSize { get; set; }
        public string PackageFileName { get; set; }       
        public AppxPackage()
        {
            BlockMap = new Dictionary<string, List<AppxBlockMapFile>>();
            Manifest = new AppxManifest();
            ParentPackage = null;
            PackageType = PACKAGE_TYPE.PACKAGE_TYPE_APPX_PACKAGE;
            UncompressedSize = 0;
            PackageFileName = string.Empty;
        }

        public bool IsInFamily(AppxPackage otherPackage)
        {
            if(Manifest.Name == otherPackage.Manifest.Name &&
                Manifest.Publisher == otherPackage.Manifest.Publisher)
            {
                return true;
            }

            return false;
        }

    }

}
