using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Windows_App_Shared_Data
{
    public class AppxPackageReader
    {
        public List<AppxPackage> MainPackages { get; set; }
        public List<AppxPackage> ResourcePackages { get; set; }
        public bool ErrorEncountered { get { return errorEncountered; } }
        public string ErrorText { get { return errorText; } }


        private string packagePath;
        private IStream stream;
        private ManualResetEvent mre;
        private bool errorEncountered;
        private string errorText;

        private bool internalPackage;

        public AppxPackageReader(string PackagePath)
        {
            packagePath = PackagePath;
            internalPackage = false;
            Init();
        }

        private AppxPackageReader(IStream Stream)
        {
            stream = Stream;
            internalPackage = true;
            Init();
        }

        private void Init()
        {
            MainPackages = new List<AppxPackage>();
            ResourcePackages = new List<AppxPackage>();
            mre = new ManualResetEvent(false);
            errorEncountered = false;
            errorText = string.Empty;
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem(Run_Async);
        }

        public static AppxManifest GetManifestFromPath(string path)
        {
            var stream = StreamFactory.CreateFileStream(path, STGM.STGM_READ);
            var type = GetAppxPackageTypeFromStream(stream);

            if (type == AppxPackage.PACKAGE_TYPE.PACKAGE_TYPE_APPX_BUNDLE)
                return GetManifestInformationForAppxBundle(stream);
            return GetManifestInformationForAppxPackage(stream);
        }

        private void Run_Async(object state)
        {
           try
           {
                IStream packageStream;

                // this function can be called from the bundle reader below or from external
                if (internalPackage)
                    packageStream = stream;
                else
                    packageStream = StreamFactory.CreateFileStream(packagePath, STGM.STGM_READ);

                AppxPackage package = GetAppxPackageFromStream(packageStream);

                if (package.PackageType == AppxPackage.PACKAGE_TYPE.PACKAGE_TYPE_APPX_BUNDLE)
                {
                    List<AppxPackage> mainPackagesFromBundle = new List<AppxPackage>();
                    List<AppxPackage> resourcePackagesFrombundle = new List<AppxPackage>();
                    foreach (AppxPackage containedPackage in (package as AppxBundlePackage).ContainedPackages)
                    {
                        if (containedPackage.PackageType == AppxPackage.PACKAGE_TYPE.PACKAGE_TYPE_APPX_RESOUCE)
                            resourcePackagesFrombundle.Add(containedPackage);

                        else if (containedPackage.PackageType == AppxPackage.PACKAGE_TYPE.PACKAGE_TYPE_APPX_PACKAGE)
                            mainPackagesFromBundle.Add(containedPackage);
                    }

                    MainPackages.Add(SelectCorrectMainPackage(mainPackagesFromBundle));
                    ResourcePackages.AddRange(resourcePackagesFrombundle);
                }
                else
                    MainPackages.Add(package);
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                errorEncountered = true;
                errorText = "Input package is corrupted. Error: '" + ex.HResult + "'";
            }
            catch (Exception ex)
            {
                errorEncountered = true;
                errorText = "Error encoutered. Error: '" + ex.HResult + "'";
            }

            // set the event
            mre.Set();
        }

        public void Wait()
        {
            mre.WaitOne();
        }

        #region Package Functions

        private List<AppxPackage> GetContainedPackagesFromBundle(IStream stream)
        {
            List<AppxPackage> containedPackages = new List<AppxPackage>();

            try
            {
                var bundleFactory = (IAppxBundleFactory)new AppxBundleFactory();
                var bundlePackageReader = bundleFactory.CreateBundleReader(stream);
                var bundleManifestReader = bundlePackageReader.GetManifest();
                var bundleManifestInfoItemsEnum = bundleManifestReader.GetPackageInfoItems();

                List<AppxPackageReader> allPackageReadersFromBundle = new List<AppxPackageReader>();
                while (bundleManifestInfoItemsEnum.GetHasCurrent())
                {
                    var bundleManifestPackageInfo = bundleManifestInfoItemsEnum.GetCurrent();

                    // get represented file
                    var appxFile = bundlePackageReader.GetPayloadPackage(bundleManifestPackageInfo.GetFileName());

                    // get the appxpackage
                    AppxPackageReader r = new AppxPackageReader(appxFile.GetStream());
                    allPackageReadersFromBundle.Add(r);
                    r.Run();

                    bundleManifestInfoItemsEnum.MoveNext();

                    Marshal.ReleaseComObject(bundleManifestPackageInfo);
                }

                foreach (AppxPackageReader r in allPackageReadersFromBundle)
                {
                    r.Wait();
                    containedPackages.AddRange(r.MainPackages);
                    containedPackages.AddRange(r.ResourcePackages);
                }

                Marshal.ReleaseComObject(bundleManifestInfoItemsEnum);
                Marshal.ReleaseComObject(bundleManifestReader);
                Marshal.ReleaseComObject(bundlePackageReader);
                Marshal.ReleaseComObject(bundleFactory);
            }
            catch
            {
                // TBD
            }
            return containedPackages;
        }
        private AppxPackage GetAppxPackageFromStream(IStream stream)
        {
            AppxPackage appxPackage;

            // determine the package type (bundle, resource or main)
            var packageType = GetAppxPackageTypeFromStream(stream);

            if (packageType == AppxPackage.PACKAGE_TYPE.PACKAGE_TYPE_APPX_BUNDLE)
            {
                appxPackage = new AppxBundlePackage();

                (appxPackage as AppxBundlePackage).ContainedPackages = GetContainedPackagesFromBundle(stream);

                // remember that this package came from a bundle
                foreach (AppxPackage containedPackage in (appxPackage as AppxBundlePackage).ContainedPackages)
                    containedPackage.ParentPackage = appxPackage;

                appxPackage.Manifest = GetManifestInformationForAppxBundle(stream);
            }
            else
            {
                if (packageType == AppxPackage.PACKAGE_TYPE.PACKAGE_TYPE_APPX_RESOUCE)
                {
                    appxPackage = new AppxResourcePackage();

                    (appxPackage as AppxResourcePackage).ResourceList = GetResourceList(stream);
                }
                else if (packageType == AppxPackage.PACKAGE_TYPE.PACKAGE_TYPE_APPX_PACKAGE)
                    appxPackage = new AppxPackage();
                else
                    throw new ArgumentOutOfRangeException("Input Package is of unknown type");

                ulong uncompressedSize = 0;

                appxPackage.BlockMap = GetBlockMapObject(stream, appxPackage, out uncompressedSize);
                appxPackage.Manifest = GetManifestInformationForAppxPackage(stream);
                appxPackage.UncompressedSize = uncompressedSize;
            }

            return appxPackage;
        }

        private static AppxManifest GetManifestInformationForAppxBundle(IStream stream)
        {
            var bundleManifest = new AppxManifest();

            try
            {
                var bundleFactory = (IAppxBundleFactory)new AppxBundleFactory();
                var bundlePackageReader = bundleFactory.CreateBundleReader(stream);
                var bundleManifestReader = bundlePackageReader.GetManifest();

                bundleManifest.Name = bundleManifestReader.GetPackageId().GetName();
                bundleManifest.Version = bundleManifestReader.GetPackageId().GetVersion();
                bundleManifest.Publisher = bundleManifestReader.GetPackageId().GetPublisher();
                bundleManifest.ResourceID = bundleManifestReader.GetPackageId().GetResourceId();
                bundleManifest.ProcessorArchitecture = bundleManifestReader.GetPackageId().GetArchitecture();

                Marshal.ReleaseComObject(bundleManifestReader);
                Marshal.ReleaseComObject(bundlePackageReader);
                Marshal.ReleaseComObject(bundleFactory);
            }
            catch
            {
                // TBD
            }

            return bundleManifest;
        }
        private static AppxManifest GetManifestInformationForAppxPackage(IStream stream)
        {
            var appxManifest = new AppxManifest();

            try
            {
                var appxFactory = (IAppxFactory)new AppxFactory();
                var appxPackageReader = appxFactory.CreatePackageReader(stream);
                var appxManifestReader = appxPackageReader.GetManifest();

                appxManifest.Name = appxManifestReader.GetPackageId().GetName();
                appxManifest.Version = appxManifestReader.GetPackageId().GetVersion();
                appxManifest.Publisher = appxManifestReader.GetPackageId().GetPublisher();
                appxManifest.ResourceID = appxManifestReader.GetPackageId().GetResourceId();
                appxManifest.ProcessorArchitecture = appxManifestReader.GetPackageId().GetArchitecture();

                Marshal.ReleaseComObject(appxManifestReader);
                Marshal.ReleaseComObject(appxPackageReader);
                Marshal.ReleaseComObject(appxFactory);
            }
            catch
            {
                // TBD
            }

            return appxManifest;
        }
        private List<IAppxManifestQualifiedResource> GetFullyQualifiedResourceList(IAppxBundleManifestPackageInfo bundleManifestPackageInfo)
        {
            List<IAppxManifestQualifiedResource> allResources = new List<IAppxManifestQualifiedResource>();

            try
            {
                var appxManifestQualifiedResourceEnum = bundleManifestPackageInfo.GetResources();
                while (appxManifestQualifiedResourceEnum.GetHasCurrent())
                {
                    allResources.Add(appxManifestQualifiedResourceEnum.GetCurrent());
                    appxManifestQualifiedResourceEnum.MoveNext();
                }

                Marshal.ReleaseComObject(appxManifestQualifiedResourceEnum);
            }
            catch
            {
                // TBD
            }

            return allResources;
        }
        private List<string> GetResourceList(IStream stream)
        {
            List<string> allResources = new List<string>();

            try
            {
                var appxFactory = (IAppxFactory)new AppxFactory();
                var appxPackageReader = appxFactory.CreatePackageReader(stream);
                var appxManifestReader = appxPackageReader.GetManifest();

                var appxManifestResourceEnum = appxManifestReader.GetResources();
                while (appxManifestResourceEnum.GetHasCurrent())
                {
                    allResources.Add(appxManifestResourceEnum.GetCurrent());
                    appxManifestResourceEnum.MoveNext();
                }

                Marshal.ReleaseComObject(appxManifestResourceEnum);
                Marshal.ReleaseComObject(appxManifestReader);
                Marshal.ReleaseComObject(appxPackageReader);
                Marshal.ReleaseComObject(appxFactory);
            }
            catch
            {
                // TBD
            }

            return allResources;
        }
        private Dictionary<string, List<AppxBlockMapFile>> GetBlockMapObject(IStream stream, AppxPackage parentPackage, out ulong UncompressedSize)
        {
            UncompressedSize = 0;

            var appxFactory = (IAppxFactory)new AppxFactory();
            var appxPackageReader = appxFactory.CreatePackageReader(stream);
            var appxBlockMapReader = appxPackageReader.GetBlockMap();
            var blockMapFilesEnum = appxBlockMapReader.GetFiles();

            var blockMapObject = new Dictionary<string, List<AppxBlockMapFile>>();
            var blockMapFilesList = new List<AppxBlockMapFile>();

            // walk through the entire block map file
            while (blockMapFilesEnum.GetHasCurrent())
            {
                // get the file node and all containing block nodes
                var nextBlockMapFile = blockMapFilesEnum.GetCurrent();
                var blockMapFile = new AppxBlockMapFile()
                {
                    FileName = nextBlockMapFile.GetName(),
                    UncompressedSize = nextBlockMapFile.GetUncompressedSize(),
                    LocalFileHeaderSize = nextBlockMapFile.GetLocalFileHeaderSize(),
                    ParentPackage = parentPackage
                };

                UncompressedSize += blockMapFile.UncompressedSize;

                // retrieve enumerator for blocks
                var allBlockMapBlocksEnum = nextBlockMapFile.GetBlocks();

                // well get the full file hash later for easy comparison
                var fullFileHashBuffer = new List<byte>();

                // get all containing blocks
                while (allBlockMapBlocksEnum.GetHasCurrent())
                {
                    var nextBlock = allBlockMapBlocksEnum.GetCurrent();

                    UInt32 bufferSize;
                    var hashBuffer = nextBlock.GetHash(out bufferSize);

                    // convert the array from BYTE ** to a byte[]
                    byte[] hashArray = new byte[bufferSize];
                    Marshal.Copy(hashBuffer, hashArray, 0, (int)bufferSize);
                    Marshal.FreeCoTaskMem(hashBuffer);

                    // add this byte range to our full file hash
                    fullFileHashBuffer.AddRange(hashArray);

                    // the array is base 64 encoded to save memory, decode
                    var hashText = Convert.ToBase64String(hashArray);

                    // capture blockmap data
                    var blockMapBlock = new AppxBlockMapBlock()
                    {
                        Hash = hashText,
                        CompressedSize = nextBlock.GetCompressedSize(),
                        ParentFile = blockMapFile
                    };

                    // save the block 
                    blockMapFile.Blocks.Add(blockMapBlock);

                    allBlockMapBlocksEnum.MoveNext();

                    Marshal.ReleaseComObject(nextBlock);
                }

                Marshal.ReleaseComObject(allBlockMapBlocksEnum);
                Marshal.ReleaseComObject(nextBlockMapFile);

                // get file hash
                blockMapFile.FinalFileHash = Helpers.GetHash(fullFileHashBuffer);

                // add the finished file
                if (!blockMapObject.ContainsKey(blockMapFile.FinalFileHash))
                    blockMapObject.Add(blockMapFile.FinalFileHash, new List<AppxBlockMapFile>());
                blockMapObject[blockMapFile.FinalFileHash].Add(blockMapFile);

                // add it to full bag - used for update comparison 
                blockMapFilesList.Add(blockMapFile);

                blockMapFilesEnum.MoveNext();
            }
            Marshal.ReleaseComObject(blockMapFilesEnum);
            Marshal.ReleaseComObject(appxBlockMapReader);
            Marshal.ReleaseComObject(appxPackageReader);
            Marshal.ReleaseComObject(appxFactory);

            return blockMapObject;
        }
        private static AppxPackage.PACKAGE_TYPE GetAppxPackageTypeFromStream(IStream stream)
        {
            var type = AppxPackage.PACKAGE_TYPE.PACKAGE_TYPE_APPX_PACKAGE;

            try
            {
                var appxFactory = (IAppxFactory)new AppxFactory();
                var appxPackageReader = appxFactory.CreatePackageReader(stream);
                var appxManifestReader = appxPackageReader.GetManifest();
                var resourceID = appxManifestReader.GetPackageId().GetResourceId();

                // determine if the package is a resource package
                if (appxManifestReader.GetProperties().GetBoolValue("ResourcePackage"))
                    return AppxPackage.PACKAGE_TYPE.PACKAGE_TYPE_APPX_RESOUCE;

                Marshal.ReleaseComObject(appxManifestReader);
                Marshal.ReleaseComObject(appxPackageReader);
                Marshal.ReleaseComObject(appxFactory);
            }
            catch (COMException ex)
            {
                // if the package is a bundle
                if (ex.HResult == Helpers.APPX_E_MISSING_REQUIRED_FILE)
                {
                    var bundleFactory = (IAppxBundleFactory)new AppxBundleFactory();
                    var bundleReader = bundleFactory.CreateBundleReader(stream);

                    type = AppxPackage.PACKAGE_TYPE.PACKAGE_TYPE_APPX_BUNDLE;

                    Marshal.ReleaseComObject(bundleReader);
                    Marshal.ReleaseComObject(bundleFactory);
                }
            }

            return type;
        }

        private AppxPackage SelectCorrectMainPackage(List<AppxPackage> mainPackagesFromBundle)
        {
            AppxPackage returnPackage = null;

            foreach (AppxPackage package in mainPackagesFromBundle)
            {
                if (package.Manifest.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_NEUTRAL)
                {
                    returnPackage = package;
                    break;
                }
                else if (package.Manifest.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_X64 && Environment.Is64BitOperatingSystem)
                {
                    returnPackage = package;
                    break;
                }
                else if (package.Manifest.ProcessorArchitecture == APPX_PACKAGE_ARCHITECTURE.APPX_PACKAGE_ARCHITECTURE_X86)
                {
                    returnPackage = package;
                    break;
                }
            }

            return returnPackage;
        }

        #endregion
    }
}
