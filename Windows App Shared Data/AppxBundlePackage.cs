using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows_App_Shared_Data
{
    public class AppxBundlePackage : AppxPackage
    {
        public List<AppxPackage> ContainedPackages { get; set; }
        public AppxBundlePackage()
        {
            PackageType = PACKAGE_TYPE.PACKAGE_TYPE_APPX_BUNDLE;
        }
    }
}
