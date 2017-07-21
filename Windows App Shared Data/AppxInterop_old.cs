using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices.ComTypes;

namespace Windows_App_Shared_Data2
{
    public class AppxInterop
    {
        public enum APPX_FOOTPRINT_FILE_TYPE
        {
            APPX_FOOTPRINT_FILE_TYPE_MANIFEST = 0,
            APPX_FOOTPRINT_FILE_TYPE_BLOCKMAP = 1,
            APPX_FOOTPRINT_FILE_TYPE_SIGNATURE = 2,
            APPX_FOOTPRINT_FILE_TYPE_CODEINTEGRITY = 3
        }

        public enum APPX_PACKAGE_ARCHITECTURE
        {
            APPX_PACKAGE_ARCHITECTURE_X86 = 0,
            APPX_PACKAGE_ARCHITECTURE_ARM = 5,
            APPX_PACKAGE_ARCHITECTURE_X64 = 9,
            APPX_PACKAGE_ARCHITECTURE_NEUTRAL = 11
        }

        [ComImport, Guid("beb94909-e451-438b-b5a7-d79e767b75d8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAppxFactory
        {
            uint CreatePackageWriter(
                [In] object outputStream,
                [In] object settings,
                [Out] object packageWriter
                );

            uint CreatePackageReader(
                [In] IStream inputStream,
                [Out] IAppxPackageReader packageReader
                );

            uint CreateManifestReader(
                [In] IStream inputStream,
                [Out] IAppxManifestReader manifestReader
                );

            uint CreateBlockMapReader(
                [In] IStream inputStream,
                [Out] IAppxBlockMapReader blockMapReader
                );

            uint CreateValidatedBlockMapReader(
                [In] IStream blockMapStream,
                [In] string fileName,
                [Out] IAppxBlockMapReader blockMapReader
                );
        }

        [ComImport, Guid("b5c49650-99bc-481c-9a34-3d53a4106708")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAppxPackageReader
        {
            uint GetBlockMap(
                [Out] IAppxBlockMapReader blockMapReader
                );

            uint GetFootprintFile(
                [In] APPX_FOOTPRINT_FILE_TYPE type,
                [Out] IAppxFile file
                );

            uint GetPayloadFile(
                [In] string fileName,
                [Out] IAppxFile file
                );

            uint GetPayloadFiles(
                [Out] IAppxFilesEnumerator filesEnumerator
                );

            uint GetManifest(
                [Out] IAppxManifestReader manifestReader
                );
        }

        [ComImport, Guid("5efec991-bca3-42d1-9ec2-e92d609ec22a")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAppxBlockMapReader
        {
            uint GetFile(
                [In] string fileName,
                [Out] IAppxBlockMapFile file
                );

            uint GetFiles(
                [Out] IAppxBlockMapFilesEnumerator enumerator
                );

            uint GetHashMethod(
                [Out] System.Uri hashMethod
                );

            uint GetStream(
                [Out] IStream blockMapStream
                );
        }

        [ComImport, Guid("02b856a2-4262-4070-bacb-1a8cbbc42305")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAppxBlockMapFilesEnumerator
        {
            uint GetCurrent(
                [Out] IAppxFile file
                );

            uint GetHasCurrent(
                [Out] bool hasCurrent
                );

            uint MoveNext(
                [Out] bool hasNext
                );
        }

        [ComImport, Guid("277672ac-4f63-42c1-8abc-beae3600eb59")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAppxBlockMapFile
        {
            uint GetBlocks(
                [Out] IAppxBlockMapBlocksEnumerator blocks
                );

            uint GetLocalFileHeaderSize(
                [Out] UInt32 lfhSize
                );

            uint GetName([Out] string name
                );

            uint GetUncompressedSize(
                [Out] UInt64 size
                );

            uint ValidateFileHash(
                [In] IStream fileStream,
                [Out] bool isValid
                );
        }

        [ComImport, Guid("6b429b5b-36ef-479e-b9eb-0c1482b49e16")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAppxBlockMapBlocksEnumerator
        {
            uint GetCurrent(
                [Out] IAppxFile file
                );

            uint GetHasCurrent(
                [Out] bool hasCurrent
                );

            uint MoveNext(
                [Out] bool hasNext
                );
        }

        [ComImport, Guid("4e1bd148-55a0-4480-a3d1-15544710637c")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAppxManifestReader
        {
            uint GetPackageId(
                [Out] IAppxManifestPackageId packageId
                );

            uint GetProperties(
                [Out] object packageProperties
                );

            uint GetPackageDependencies(
                [Out] object dependencies
                );

            uint GetCapabilities(
                [Out] object capabilities
                );

            uint GetResources(
                [Out] object resources
                );

            uint GetDeviceCapabilities(
                [Out] object deviceCapabilities
                );

            uint GetPrerequisite(
                [In] string name,
                [Out] UInt64 value
                );

            uint GetApplications(
                [Out] object applications
                );

            uint GetStream(
                [Out] IStream manifestStream
                );
        }

        [ComImport, Guid("4e1bd148-55a0-4480-a3d1-15544710637c")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAppxManifestPackageId
        {
            uint GetName(
                [Out] string name
                );

            uint GetArchitecture(
                [Out] APPX_PACKAGE_ARCHITECTURE architecture
                );

            uint GetPublisher(
                [Out] string publisher
                );

            uint GetVersion(
                [Out] UInt64 packageVersion
                );

            uint GetResourceId(
                [Out] string resourceId
                );

            uint ComparePublisher(
                [In] string other,
                [Out] bool isSame
                );

            uint GetPackageFullName(
                [Out] string packageFullName
                );

            uint GetPackageFamilyName(
                [Out] string packageFamilyName
                );
        }

        [ComImport, Guid("91df827b-94fd-468f-827b-57f41b2f6f2e")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAppxFile
        {
            uint GetCompressionOption(
                [Out] object compressionOption
                );

            uint GetContentType(
                [Out] string contentType
                );
            uint GetName(
                [Out] string fileName
                );
            uint GetSize(
                [Out] UInt64 size
                );

            uint GetStream(
                [Out] IStream stream
                );
        }

        [ComImport, Guid("f007eeaf-9831-411c-9847-917cdc62d1fe")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IAppxFilesEnumerator
        {
            uint GetCurrent(
                [Out] IAppxFile file
                );
            uint GetHasCurrent(
                [Out] bool hasCurrent
                );
            uint MoveNext(
                [Out] bool hasNext
                );

        }
    }
}
