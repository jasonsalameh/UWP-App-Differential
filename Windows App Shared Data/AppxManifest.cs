using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows_App_Shared_Data
{
    public class AppxManifest
    {
        private Version _version;
        private ulong _fullVersion;

        public string Name { get; set; }
        public string Publisher { get; set; }
        public string ResourceID { get; set; }
        public ulong Version
        {
            get
            {
                return _fullVersion;
            }
            set
            {
                _fullVersion = value;

                // version is 64 bits comprised of 4 16bit words
                int maj = (int)((_fullVersion << 0) >> 48);
                int min = (int)((_fullVersion << 16) >> 48);
                int bld = (int)((_fullVersion << 32) >> 48);
                int rev = (int)((_fullVersion << 48) >> 48);

                _version = new System.Version(maj, min, bld, rev);
            }
        }
        public APPX_PACKAGE_ARCHITECTURE ProcessorArchitecture { get; set; }

        public AppxManifest()
        {
            Name = string.Empty;
            _fullVersion = 0;
            Publisher = string.Empty;
            ResourceID = string.Empty;
            ProcessorArchitecture = APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_NEUTRAL;
        }

        public string FullName()
        {
            return Name + "_" + _version.ToString() + "_" + ResourceID + "_" + GetArch(ProcessorArchitecture);
        }

        private string GetArch(APPX_PACKAGE_ARCHITECTURE arch)
        {
            if (arch == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_ARM)
                return "arm";
            else if (arch == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_ARM64)
                return "arm64";
            else if (arch == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_NEUTRAL)
                return "neutral";
            else if (arch == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_X64)
                return "x64";
            else if (arch == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_X86)
                return "x86";

            return "???";
        }

        public string FriendlyName()
        {
            return Name + "_" + _version.ToString();
        }

        public bool IsUpdate(AppxManifest incomming, out string errorMsg)
        {
            errorMsg = string.Empty;

            // different name
            if (this.Name.CompareTo(incomming.Name) != 0)
            {
                errorMsg = "Packages have a different Name.";
                return false;
            }

            // different publisher
            if (this.Publisher.CompareTo(incomming.Publisher) != 0)
            {
                errorMsg = "Packages have a different Publisher.";
                return false;
            }

            // different resource ID
            if (this.ResourceID != null && incomming.ResourceID != null &&
                this.ResourceID.CompareTo(incomming.ResourceID) != 0)
            {
                errorMsg = "Packages have a different ResourceID.";
                return false;
            }

            // this is x86 or x64 and that is arm or arm64
            if ((this.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_X64 || this.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_X86) &&
                incomming.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_ARM || incomming.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_ARM64)
            {
                errorMsg = "Packages have incompatible architectures.";
                return false;
            }

            // this is arm or arm64 and that is x86 or x64
            if ((this.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_ARM || this.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_ARM64) &&
                incomming.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_X64 || incomming.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_X86)
            {
                errorMsg = "Packages have incompatible architectures.";
                return false;
            }

            // same version package
            if (ComparePackageVersions(incomming) == 0)
            {
                errorMsg = "Packages are the same version.";
                return false;
            }

            return true;
        }

        // return
        //        -1 if other package is newer
        //         0 if they are the same version
        //         1 if this package is newer
        public int ComparePackageVersions(AppxManifest incomming)
        {
            return Version.CompareTo(incomming.Version);
        }
    }
}
