using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows_App_Shared_Data
{
    public class AppxResourcePackage : AppxPackage
    {
        public List<IAppxManifestQualifiedResource> QualifiedResourceList;
        public List<string> ResourceList;
        public AppxResourcePackage()
        {
            PackageType = PACKAGE_TYPE.PACKAGE_TYPE_APPX_RESOUCE;
        }
    }
}
