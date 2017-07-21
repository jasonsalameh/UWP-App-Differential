using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Windows_App_Shared_Data
{
    
    public class StreamFactory
    {
        // UINT -> enum  
        public static IStream CreateFileStream(String fileName, STGM mode, UInt32 attributes = 0x1, bool create = false)
        {
            return SHCreateStreamOnFileEx(fileName, mode, attributes, create, null);
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern IStream SHCreateStreamOnFileEx([In] String fileName, [In] STGM mode, [In] UInt32 attributes, [In] bool create, [In] IStream template);
    }

    [Flags]
    public enum STGM
    {
        STGM_READ = 0x0,
        STGM_WRITE = 0x1,
        STGM_READWRITE = 0x2
    }


    // Defined in AppxPackaging.h  
    public enum APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE
    {
        APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_APPLICATION = 0,
        APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE_RESOURCE = 1
    }

    // Defined in AppxPackaging.h  
    public enum APPX_PACKAGE_ARCHITECTURE
    {
        APPX_PACKAGE_ARCHITECTURE_X86 = 0,
        APPX_PACKAGE_ARCHITECTURE_ARM = 5,
        APPX_PACKAGE_ARCHITECTURE_X64 = 9,
        APPX_PACKAGE_ARCHITECTURE_NEUTRAL = 11,
        APPX_PACKAGE_ARCHITECTURE_ARM64 = 12
    }

    // Defined in AppxPackaging.h  
    public enum APPX_BUNDLE_FOOTPRINT_FILE_TYPE
    {
        APPX_BUNDLE_FOOTPRINT_FILE_TYPE_FIRST = 0,
        APPX_BUNDLE_FOOTPRINT_FILE_TYPE_MANIFEST = 0,
        APPX_BUNDLE_FOOTPRINT_FILE_TYPE_BLOCKMAP = 1,
        APPX_BUNDLE_FOOTPRINT_FILE_TYPE_SIGNATURE = 2,
        APPX_BUNDLE_FOOTPRINT_FILE_TYPE_LAST = 2
    }

    // Defined in AppxPackaging.h  
    public enum DX_FEATURE_LEVEL
    {
        DX_FEATURE_LEVEL_UNSPECIFIED = 0,
        DX_FEATURE_LEVEL_9 = 1,
        DX_FEATURE_LEVEL_10 = 2,
        DX_FEATURE_LEVEL_11 = 3
    }

    // Defined in AppxPackaging.h  
    public enum APPX_FOOTPRINT_FILE_TYPE
    {
        APPX_FOOTPRINT_FILE_TYPE_MANIFEST = 0,
        APPX_FOOTPRINT_FILE_TYPE_BLOCKMAP = 1,
        APPX_FOOTPRINT_FILE_TYPE_SIGNATURE = 2,
        APPX_FOOTPRINT_FILE_TYPE_CODEINTEGRITY = 3,
    }

    public class APPX_ENCRYPTED_PACKAGE_SETTINGS
    {
        public UInt32 keyLength { get; set; }
        public string encryptionAlgorithm { get; set; }
        public bool useDiffusion { get; set; }
        public Uri blockMapHashAlgorithm { get; set; }
    }

    public class APPX_KEY_INFO
    {
        public UInt32 keyLength { get; set; }
        public UInt32 keyIdLength { get; set; }
        public byte[] key { get; set; }
        public byte[] keyId { get; set; }
    }

    public class APPX_ENCRYPTED_EXEMPTIONS
    {
        public UInt64 count { get; set; }
        public string plainTextFiles { get; set; }
    }


    [ComImport, Guid("5842a140-ff9f-4166-8f5c-62f5b7b0c781")]
    public class AppxFactory 
    {
    }

    [ComImport, Guid("DC664FDD-D868-46EE-8780-8D196CB739F7")]
    public class AppxEncryptionFactory
    {
    }


    [Guid("f007eeaf-9831-411c-9847-917cdc62d1fe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxFilesEnumerator : IDisposable
    {
        IAppxFile GetCurrent();
        bool GetHasCurrent();
        bool MoveNext();
    }

    
    [Guid("b43bbcf9-65a6-42dd-bac0-8c6741e7f5a4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxManifestPackageDependenciesEnumerator : IDisposable
    {
        IAppxManifestPackageDependency GetCurrent();
        bool GetHasCurrent();
        bool MoveNext();
    }

    
    [Guid("e4946b59-733e-43f0-a724-3bde4c1285a0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxManifestPackageDependency : IDisposable
    {
        [return: MarshalAs(UnmanagedType.LPWStr)]
        String GetName();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        String GetPublisher();
        UInt64 GetMinVersion();
    }

    
    [Guid("BBA65864-965F-4A5F-855F-F074BDBF3A7B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxBundleFactory : IDisposable
    {
        IAppxBundleWriter CreateBundleWriter([In] IStream outputStream, [In] UInt64 bundleVersion);
        IAppxBundleReader CreateBundleReader([In] IStream inputStream);
        IAppxBundleManifestReader CreateBundleManifestReader([In] IStream inputStream);
    }

    [ComImport, Guid("378E0446-5384-43B7-8877-E7DBDD883446")]
    public class AppxBundleFactory 
    {
    }

    
    [Guid("beb94909-e451-438b-b5a7-d79e767b75d8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxFactory : IDisposable
    {
        IAppxPackageWriter CreatePackageWriter([In] IStream outputStream, [In] APPX_PACKAGE_SETTINGS settings);
        IAppxPackageReader CreatePackageReader([In] IStream inputStream);
        IAppxManifestReader CreateManifestReader([In] IStream inputStream);
        IAppxBlockMapReader CreateBlockMapReader([In] IStream inputStream);
        IAppxBlockMapReader CreateValidatedBlockMapReader([In] IStream blockMapStream, [In] String signatureFileName);
    }

    //[Guid("80E8E04D-8C88-44AE-A011-7CADF6FB2E72"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //public interface IAppxEncryptionFactory : IDisposable
    //{
    //    void EncryptPackage([In] IStream inputStream, [In] IStream outputStream, [In] APPX_ENCRYPTED_PACKAGE_SETTINGS settings, [In] APPX_KEY_INFO keyInfo, [In] APPX_ENCRYPTED_EXEMPTIONS exemptedFiles);
    //    void DecryptPackage([In] IStream inputStream, [In] IStream outputStream, [In] APPX_KEY_INFO keyInfo);
    //    IAppxEncyptedPackageWriter CreateEncyptedPackageWriter([In] IStream outputStream, [In] APPX_PACKAGE_SETTINGS settings, [In] APPX_ENCRYPTED_PACKAGE_SETTINGS settings, [In] APPX_KEY_INFO keyInfo, [In] APPX_ENCRYPTED_EXEMPTIONS exemptedFiles);
    //    IAppxEncyptedPackageReader CreateEncyptedPackageWriter([In] IStream outputStream, [In] APPX_PACKAGE_SETTINGS settings, [In] APPX_ENCRYPTED_PACKAGE_SETTINGS settings, [In] APPX_KEY_INFO keyInfo, [In] APPX_ENCRYPTED_EXEMPTIONS exemptedFiles);

    //}


    [Guid("DD75B8C0-BA76-43B0-AE0F-68656A1DC5C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxBundleReader : IDisposable
    {
        IAppxFile GetFootprintFile([In] APPX_BUNDLE_FOOTPRINT_FILE_TYPE fileType);
        IAppxBlockMapReader GetBlockMap();
        IAppxBundleManifestReader GetManifest();
        IAppxFilesEnumerator GetPayloadPackages();
        IAppxFile GetPayloadPackage([In, MarshalAs(UnmanagedType.LPWStr)] String fileName);
    }

    
    [Guid("CF0EBBC1-CC99-4106-91EB-E67462E04FB0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxBundleManifestReader : IDisposable
    {
        IAppxManifestPackageId GetPackageId();
        IAppxBundleManifestPackageInfoEnumerator GetPackageInfoItems();
        IStream GetStream();
    }

    
    [Guid("54CD06C1-268F-40BB-8ED2-757A9EBAEC8D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxBundleManifestPackageInfo : IDisposable
    {
        APPX_BUNDLE_PAYLOAD_PACKAGE_TYPE GetPackageType();
        IAppxManifestPackageId GetPackageId();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        String GetFileName();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        UInt64 GetOffset();
        UInt64 GetSize();
        IAppxManifestQualifiedResourcesEnumerator GetResources();
    }

    
    [Guid("F9B856EE-49A6-4E19-B2B0-6A2406D63A32"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxBundleManifestPackageInfoEnumerator : IDisposable
    {
        IAppxBundleManifestPackageInfo GetCurrent();
        bool GetHasCurrent();
        bool MoveNext();
    }

    
    [Guid("b5c49650-99bc-481c-9a34-3d53a4106708"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxPackageReader : IDisposable
    {
        IAppxBlockMapReader GetBlockMap();
        IAppxFile GetFootprintFile([In] APPX_FOOTPRINT_FILE_TYPE type);
        IAppxFile GetPayloadFile([In, MarshalAs(UnmanagedType.LPWStr)] String fileName);
        IAppxFilesEnumerator GetPayloadFiles();
        IAppxManifestReader GetManifest();
    }

    
    [Guid("4e1bd148-55a0-4480-a3d1-15544710637c"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxManifestReader : IDisposable
    {
        IAppxManifestPackageId GetPackageId();
        IAppxManifestProperties GetProperties();
        IAppxManifestPackageDependenciesEnumerator GetPackageDependencies();
        APPX_CAPABILITIES GetCapabilities();
        IAppxManifestResourcesEnumerator GetResources();
        IAppxManifestDeviceCapabilitiesEnumerator GetDeviceCapabilities();
        UInt64 GetPrerequisite([In, MarshalAs(UnmanagedType.LPWStr)] String name);
        IAppxManifestApplicationsEnumerator GetApplications();
        IStream GetStream();
    }

    
    [Guid("d06f67bc-b31d-4eba-a8af-638e73e77b4d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxManifestReader2 : IDisposable
    {
        IAppxManifestPackageId GetPackageId();
        IAppxManifestProperties GetProperties();
        IAppxManifestPackageDependenciesEnumerator GetPackageDependencies();
        APPX_CAPABILITIES GetCapabilities();
        IAppxManifestResourcesEnumerator GetResources();
        IAppxManifestDeviceCapabilitiesEnumerator GetDeviceCapabilities();
        UInt64 GetPrerequisite([In, MarshalAs(UnmanagedType.LPWStr)] String name);
        IAppxManifestApplicationsEnumerator GetApplications();
        IStream GetStream();
        IAppxManifestQualifiedResourcesEnumerator GetQualifiedResources();
    }

    
    [Guid("283ce2d7-7153-4a91-9649-7a0f7240945f"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxManifestPackageId : IDisposable
    {
        [return: MarshalAs(UnmanagedType.LPWStr)]
        String GetName();
        APPX_PACKAGE_ARCHITECTURE GetArchitecture();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        String GetPublisher();
        UInt64 GetVersion();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        String GetResourceId();
        bool ComparePublisher([In, MarshalAs(UnmanagedType.LPWStr)] String otherPublisher);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        String GetPackageFullName();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        String GetPackageFamilyName();
    }

    
    [Guid("91df827b-94fd-468f-827b-57f41b2f6f2e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxFile : IDisposable
    {
        APPX_COMPRESSION_OPTION GetCompressionOption();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        String GetContentType();
        [return: MarshalAs(UnmanagedType.LPWStr)]
        String GetName();
        UInt64 GetSize();
        IStream GetStream();
    }

    
    [Guid("03faf64d-f26f-4b2c-aaf7-8fe7789b8bca"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxManifestProperties : IDisposable
    {
        bool GetBoolValue(string name);
        void GetStringValue(string name, [Out, MarshalAs(UnmanagedType.LPWStr)] out string value);
    }

    [Guid("de4dfbbd-881a-48bb-858c-d6f2baeae6ed"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxManifestResourcesEnumerator : IDisposable
    {
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetCurrent();
        bool GetHasCurrent();
        bool MoveNext();
    }

    
    [Guid("8ef6adfe-3762-4a8f-9373-2fc5d444c8d2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxManifestQualifiedResourcesEnumerator : IDisposable
    {
        IAppxManifestQualifiedResource GetCurrent();
        bool GetHasCurrent();
        bool MoveNext();
    }


    
    [Guid("3b53a497-3c5c-48d1-9ea3-bb7eac8cd7d4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxManifestQualifiedResource : IDisposable
    {
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetLanguage();
        UInt32 GetScale();
        DX_FEATURE_LEVEL GetDXFeatureLevel();
    }

    
    [Guid("277672ac-4f63-42c1-8abc-beae3600eb59"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxBlockMapFile : IDisposable
    {

        IAppxBlockMapBlocksEnumerator GetBlocks();

        UInt32 GetLocalFileHeaderSize();

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetName();

        UInt64 GetUncompressedSize();

        bool ValidateFileHash([In] IStream fileStream);
    }


    
    [Guid("5efec991-bca3-42d1-9ec2-e92d609ec22a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxBlockMapReader : IDisposable
    {
        IAppxBlockMapFile GetFile([In, MarshalAs(UnmanagedType.LPWStr)] string filename);

        IAppxBlockMapFilesEnumerator GetFiles();

        IUri GetHashMethod();

        IStream GetStream();
    }

    
    [Guid("02b856a2-4262-4070-bacb-1a8cbbc42305"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxBlockMapFilesEnumerator : IDisposable
    {
        IAppxBlockMapFile GetCurrent();
        bool GetHasCurrent();
        bool MoveNext();
    }

    
    [Guid("6b429b5b-36ef-479e-b9eb-0c1482b49e16"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxBlockMapBlocksEnumerator : IDisposable
    {
        IAppxBlockMapBlock GetCurrent();
        bool GetHasCurrent();
        bool MoveNext();
    }

    [Guid("75cf3930-3244-4fe0-a8c8-e0bcb270b889"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppxBlockMapBlock : IDisposable
    {
        IntPtr GetHash([Out] out UInt32 bufferSize);
        UInt32 GetCompressedSize();
    }


    // Empty declarations of unused types for building project  
    public enum APPX_CAPABILITIES { }
    public enum APPX_COMPRESSION_OPTION { }
    public enum APPX_PACKAGE_SETTINGS { }
    public interface IAppxPackageWriter { }
    public interface IAppxManifestDeviceCapabilitiesEnumerator { }
    public interface IAppxManifestApplicationsEnumerator { }
    public interface IAppxBundleWriter { }
    public interface IUri { }
}