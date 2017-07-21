using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.Collections;

namespace Windows_App_Shared_Data
{

    public partial class MainWindow : Window
    {
        private string _prevOpenFileLocation;
        private List<string> _packagesToAnalyze;

        public MainWindow()
        {
            InitializeComponent();

            _prevOpenFileLocation = string.Empty;
            _packagesToAnalyze = new List<string>();

            VersionInfo.Text = "Version: " + GetRunningVersion();
        }

        private Version GetRunningVersion()
        {
            try
            {
                return System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        private void Analyze(object sender, RoutedEventArgs e)
        {
            if (!checkPackageLocations(_packagesToAnalyze))
                return;

            AnalyzeProgress.IsIndeterminate = true;
            AnalyzeButton.IsEnabled = false;
            BrowseButton.IsEnabled = false;

            // create a clone to avoid in use issues
            if (IsUpdate.IsChecked == true)
                ThreadPool.QueueUserWorkItem(Analyze_Async_Update, Helpers.Clone(_packagesToAnalyze));
            else
                ThreadPool.QueueUserWorkItem(Analyze_Async_Compare, Helpers.Clone(_packagesToAnalyze));
        }

        private void Analyze_Async_Update(object state)
        {
            AppxPackageComparisonResult result = new AppxPackageComparisonResult();

            try
            {
                List<string> packagePaths = state as List<string>;

                // make sure there are only two packages
                if (packagePaths.Count > 2)
                {
                    ShowError("Too many packages", "There can only be 2 packages selected for an update check.");
                    return;
                }
                else if (packagePaths.Count < 2)
                {
                    ShowError("Too few packages", "There must be 2 packages selected for an update check.");
                    return;
                }

                string firstPackagePath = packagePaths[0];
                string secondPackagePath = packagePaths[1];

                // make sure they're both the same package type
                if(0 != Path.GetExtension(firstPackagePath).ToLower()
                    .CompareTo(
                    Path.GetExtension(secondPackagePath).ToLower()))
                {
                    ShowError("Different Package Types", "The packages must be of the same type (e.g. both .appxbundle or both .appx)");
                    return;
                }

                AppxManifest firstPackageManifest = AppxPackageReader.GetManifestFromPath(firstPackagePath);
                AppxManifest secondPackageManifest = AppxPackageReader.GetManifestFromPath(secondPackagePath);

                string errorMsg;
                if(!firstPackageManifest.IsUpdate(secondPackageManifest, out errorMsg))
                {
                    ShowError("Identities not suited for update",errorMsg);
                    return;
                }

                AppxPackageReader incommingPackageReader;
                AppxPackageReader outgoingpackageReader;

                // firstPackageManifest is incomming (newer)
                if (firstPackageManifest.ComparePackageVersions(secondPackageManifest) > 0)
                {
                    incommingPackageReader = new AppxPackageReader(firstPackagePath);
                    outgoingpackageReader = new AppxPackageReader(secondPackagePath);
                }
                // secondPackageManifest is incomming (newer)
                else
                {
                    outgoingpackageReader = new AppxPackageReader(firstPackagePath);
                    incommingPackageReader = new AppxPackageReader(secondPackagePath);
                }

                // read the packages
                incommingPackageReader.Run();
                outgoingpackageReader.Run();

                // get all the packages
                List<AppxPackage> incommingPackageSet = new List<AppxPackage>();
                List<AppxPackage> outgoingPackageSet = new List<AppxPackage>();

                incommingPackageReader.Wait();
                incommingPackageSet.AddRange(incommingPackageReader.MainPackages);
                incommingPackageSet.AddRange(incommingPackageReader.ResourcePackages);

                outgoingpackageReader.Wait();
                outgoingPackageSet.AddRange(outgoingpackageReader.MainPackages);
                outgoingPackageSet.AddRange(outgoingpackageReader.ResourcePackages);

                // get the list of block map files
                Dictionary<string, List<AppxBlockMapFile>> incommingPackageFiles = new Dictionary<string, List<AppxBlockMapFile>>();
                foreach (AppxPackage p in incommingPackageSet)
                {
                    incommingPackageFiles = incommingPackageFiles.Concat(p.BlockMap)
                                    .GroupBy(kvp => kvp.Key)
                                    .ToDictionary(g => g.Key, g => g.SelectMany(v => v.Value).ToList());
                }

                Dictionary<string, List<AppxBlockMapFile>> outgoingPackageFiles = new Dictionary<string, List<AppxBlockMapFile>>();
                foreach (AppxPackage p in outgoingPackageSet)
                {
                    outgoingPackageFiles = outgoingPackageFiles.Concat(p.BlockMap)
                                    .GroupBy(kvp => kvp.Key)
                                    .ToDictionary(g => g.Key, g => g.SelectMany(v => v.Value).ToList());
                }


                // Determine the hardlinked files
                List<string> sameFileHashes = new List<string>();
                foreach (string key in incommingPackageFiles.Keys)
                {
                    // if it's the same
                    if (outgoingPackageFiles.ContainsKey(key))
                    {
                        sameFileHashes.Add(key);
                        foreach (AppxBlockMapFile a in incommingPackageFiles[key])
                            result.BytesHardlinked += a.UncompressedSize;

                        ChangeResult cr = new ChangeResult()
                        {
                            Package = incommingPackageFiles[key].First().ParentPackage.Manifest.FullName(),
                            FileName = incommingPackageFiles[key].First().FileName,
                            BlockHash = incommingPackageFiles[key].First().FinalFileHash,
                            Size = incommingPackageFiles[key].First().UncompressedSize,
                        };

                        result.FileHardLinkChangeResults.Add(cr.FileName, cr);
                    }
                }

                // remove hardlinked files
                 foreach(string key in sameFileHashes)
                {
                    incommingPackageFiles.Remove(key);
                    outgoingPackageFiles.Remove(key);
                }

                //get full list of blocks
                Dictionary<string, List<AppxBlockMapBlock>> incommingPackageBlocks = new Dictionary<string, List<AppxBlockMapBlock>>();
                Dictionary<string, List<AppxBlockMapBlock>> outgoingPackageBlocks = new Dictionary<string, List<AppxBlockMapBlock>>();

                foreach (List<AppxBlockMapFile> list in incommingPackageFiles.Values)
                    foreach (AppxBlockMapFile file in list)
                        foreach (AppxBlockMapBlock block in file.Blocks)
                        {
                            if(!incommingPackageBlocks.ContainsKey(block.Hash))
                                incommingPackageBlocks.Add(block.Hash, new List<AppxBlockMapBlock>());
                            incommingPackageBlocks[block.Hash].Add(block);
                        }

                foreach (List<AppxBlockMapFile> list in outgoingPackageFiles.Values)
                    foreach (AppxBlockMapFile file in list)
                        foreach (AppxBlockMapBlock block in file.Blocks)
                        {
                            if (!outgoingPackageBlocks.ContainsKey(block.Hash))
                                outgoingPackageBlocks.Add(block.Hash, new List<AppxBlockMapBlock>());
                            outgoingPackageBlocks[block.Hash].Add(block);
                        }

                // Determine Copied / Downloaded
                foreach (string hash in incommingPackageBlocks.Keys)
                {
                    // if its going to be copied
                    if (outgoingPackageBlocks.ContainsKey(hash))
                    {
                        result.BlocksCopied += incommingPackageBlocks[hash].First().CompressedSize * (ulong)incommingPackageBlocks[hash].Count;

                        ChangeResult cr = new ChangeResult()
                        {
                            Package = incommingPackageBlocks[hash].First().ParentFile.ParentPackage.Manifest.FullName(),
                            FileName = incommingPackageBlocks[hash].First().ParentFile.FileName,
                            BlockHash = incommingPackageBlocks[hash].First().Hash,
                            Size = incommingPackageBlocks[hash].First().ParentFile.UncompressedSize,
                        };

                        if(!result.BlockCopiedChangeResults.ContainsKey(cr.FileName))
                            result.BlockCopiedChangeResults.Add(cr.FileName, cr);
                    }

                    // if its going to be downloaded
                    else if (!outgoingPackageBlocks.ContainsKey(hash))
                    {
                        result.BlocksDownloaded += incommingPackageBlocks[hash].First().CompressedSize * (ulong)incommingPackageBlocks[hash].Count;

                        ChangeResult cr = new ChangeResult()
                        {
                            Package = incommingPackageBlocks[hash].First().ParentFile.ParentPackage.Manifest.FullName(),
                            FileName = incommingPackageBlocks[hash].First().ParentFile.FileName,
                            BlockHash = incommingPackageBlocks[hash].First().Hash,
                            Size = incommingPackageBlocks[hash].First().ParentFile.UncompressedSize,
                        };

                        if (!result.BlockDownloadChangeResults.ContainsKey(cr.FileName))
                            result.BlockDownloadChangeResults.Add(cr.FileName, cr);
                    }
                }

                foreach (string hash in outgoingPackageBlocks.Keys)
                {
                    // if its going to be deleted
                    if (!incommingPackageBlocks.ContainsKey(hash))
                    {
                        result.BlocksDeleted += outgoingPackageBlocks[hash].First().CompressedSize * (ulong)outgoingPackageBlocks[hash].Count;

                        ChangeResult cr = new ChangeResult()
                        {
                            Package = outgoingPackageBlocks[hash].First().ParentFile.ParentPackage.Manifest.FullName(),
                            FileName = outgoingPackageBlocks[hash].First().ParentFile.FileName,
                            BlockHash = outgoingPackageBlocks[hash].First().Hash,
                            Size = outgoingPackageBlocks[hash].First().ParentFile.UncompressedSize,
                        };

                        if (!result.BlockDeletedChangeResults.ContainsKey(cr.FileName))
                            result.BlockDeletedChangeResults.Add(cr.FileName, cr);
                    }
                }
            }
            catch (Exception ex)
            { }

            this.Dispatcher.BeginInvoke(new OutputUpdateComparisonDataToUIDelegate(OutputUpdateComparisonDataToUI), result);

        }

        private void Analyze_Async_Compare(object state)
        {
            AppxPackageComparisonResult resultNoResource = new AppxPackageComparisonResult();
            AppxPackageComparisonResult resultWithResource = new AppxPackageComparisonResult();

            try
            {
                List<string> packagePaths = state as List<string>;

                List<AppxPackageReader> readerList = new List<AppxPackageReader>();
                List<AppxPackage> allMainPackages = new List<AppxPackage>();
                List<AppxPackage> allResourcePackages = new List<AppxPackage>();

                foreach (string path in packagePaths)
                {
                    AppxPackageReader r = new AppxPackageReader(path);
                    readerList.Add(r);
                    r.Run(); // async
                }

                foreach (AppxPackageReader r in readerList)
                {
                    r.Wait(); // make sure package reader has completed

                    if(r.ErrorEncountered)
                    {
                        ShowError("Error Reading Package.", r.ErrorText);
                        return;
                    }

                    allMainPackages.AddRange(r.MainPackages);
                    allResourcePackages.AddRange(r.ResourcePackages);
                }

                List<AppxPackage> allPackages = new List<AppxPackage>();
                allPackages.AddRange(allMainPackages);
                allPackages.AddRange(allResourcePackages);

                // Get the result for no resource packages
                resultNoResource = GetInterPackageDupFiles(resultNoResource, allMainPackages);
                resultNoResource = GetCrossPackageDeDupdFiles(resultNoResource, allMainPackages);
                resultNoResource = AnalyzeFileSpaceSavings(resultNoResource, allMainPackages);

                // Get the result with resource packages
                resultWithResource = GetInterPackageDupFiles(resultWithResource, allResourcePackages);
                resultWithResource = GetCrossPackageDeDupdFiles(resultWithResource, allPackages);
                resultWithResource = AnalyzeFileSpaceSavings(resultWithResource, allPackages);

            }
            catch
            {
                // TBD
            }

            // update UI on main thread
            this.Dispatcher.BeginInvoke(new OutputComparisonDataToUIDelegate(OutputComparisonDataToUI), resultNoResource, resultWithResource);
        }

        #region Package File Operations
        private AppxPackageComparisonResult GetInterPackageDupFiles(AppxPackageComparisonResult result, List<AppxPackage> packages)
        {
            foreach (AppxPackage package in packages)
            {
                // Iterate through each file in the block map
                foreach (string fileHash in package.BlockMap.Keys)
                {
                    // if we find something that has more than one file hash (more than one of the same file)
                    // remember it
                    if (package.BlockMap[fileHash].Count() > 1)
                    {
                        if (!result.InterPackageDuplicateFiles.ContainsKey(package))
                            result.InterPackageDuplicateFiles.Add(package, new List<List<AppxBlockMapFile>>());
                        result.InterPackageDuplicateFiles[package].Add(package.BlockMap[fileHash]);
                    }
                }
            }
            return result;
        }

        private AppxPackageComparisonResult GetCrossPackageDeDupdFiles(AppxPackageComparisonResult result, List<AppxPackage> packages)
        {
            if (packages.Count <= 1)
                return result;

            foreach (AppxPackage package in packages)
            {
                // TBD : if the packages are the same package but different architecture we'll still use all of them.
                // merge all lists together
                result.CrossPackageDeDupdFiles = result.CrossPackageDeDupdFiles
                                        .Concat(package.BlockMap)
                                        .GroupBy(kvp => kvp.Key)
                                        .ToDictionary(g => g.Key, g => g.SelectMany(v => v.Value).ToList());
            }
            return result;
        }
        #endregion

        #region Analysis
        private AppxPackageComparisonResult AnalyzeFileSpaceSavings(AppxPackageComparisonResult result, List<AppxPackage> allAppxPackages)
        {
            // Unoptimized Size of Apps
            foreach (AppxPackage package in allAppxPackages)
                result.UnoptimizedSizeOfApps += package.UncompressedSize;

            // Optimzied Size of Apps
            foreach (string fileHash in result.CrossPackageDeDupdFiles.Keys)
                result.OptimizedSizeOfApps += result.CrossPackageDeDupdFiles[fileHash].First().UncompressedSize;

            return result;
        }

        #endregion

        #region UI Updates

        private delegate void OutputUpdateComparisonDataToUIDelegate(AppxPackageComparisonResult result);
        private async void OutputUpdateComparisonDataToUI(AppxPackageComparisonResult result)
        {
            try
            {
                //FileGrid.ItemsSource = PopulateComparisonGridUpdate(result);

                string filename = @"c:\temp\output.txt";

                File.AppendAllText(filename, "HardLinked Files" + Environment.NewLine);
                foreach(ChangeResult cr in result.FileHardLinkChangeResults.Values)
                    File.AppendAllText(filename, "Package: " + cr.Package + "\tFile: " + cr.FileName + "\tSize: " + cr.Size + Environment.NewLine);

                File.AppendAllText(filename, "" + Environment.NewLine);
                File.AppendAllText(filename, "" + Environment.NewLine);
                File.AppendAllText(filename, "" + Environment.NewLine);

                File.AppendAllText(filename, "Downloaded Files" + Environment.NewLine);
                foreach (ChangeResult cr in result.BlockDownloadChangeResults.Values)
                    File.AppendAllText(filename, "Package: " + cr.Package + "\tFile: " + cr.FileName + "\t\t\tSize: " + cr.Size + Environment.NewLine);

                File.AppendAllText(filename, "" + Environment.NewLine);
                File.AppendAllText(filename, "" + Environment.NewLine);
                File.AppendAllText(filename, "" + Environment.NewLine);

                File.AppendAllText(filename, "Files with similar blocks" + Environment.NewLine);
                foreach (ChangeResult cr in result.BlockCopiedChangeResults.Values)
                    File.AppendAllText(filename, "Package: " + cr.Package + "\tFile: " + cr.FileName + "\t\t\tSize: " + cr.Size + Environment.NewLine);

                File.AppendAllText(filename, "" + Environment.NewLine);
                File.AppendAllText(filename, "" + Environment.NewLine);
                File.AppendAllText(filename, "" + Environment.NewLine);

                File.AppendAllText(filename, "Deleted Files" + Environment.NewLine);
                foreach (ChangeResult cr in result.BlockDeletedChangeResults.Values)
                    File.AppendAllText(filename, "Package: " + cr.Package + "\tFile: " + cr.FileName + "\t\t\tSize: " + cr.Size + Environment.NewLine);

                OptimizedSizeOfAppsNoResource.Text = "Hardlinked ";
                UnoptimizedSizeOfAppsNoResource.Text = "Downloaded ";
                OptimizedSizeOfAppsWithResource.Text = "Deleted ";
                UnoptimizedSizeOfAppsWithResource.Text = "Blocks Copied ";

                PopulateUI(result.BytesHardlinked, result.BlocksDownloaded, result.BlocksDeleted, result.BlocksCopied);

                NameNoRP.Text = "";
                NameWithRP.Text = "";

                ResetUI();
            }
            catch (Exception ex)
            {
                // TBD
            }
        }

        private delegate void OutputComparisonDataToUIDelegate(AppxPackageComparisonResult resultNoResource, AppxPackageComparisonResult resultWithResource);
        private void OutputComparisonDataToUI(AppxPackageComparisonResult resultNoResource, AppxPackageComparisonResult resultWithResource)
        {
            try
            {
                FileGrid.ItemsSource = PopulateComparisonGrid(resultWithResource);

                NameNoRP.Text = "No Resource Packs:";
                OptimizedSizeOfAppsNoResource.Text = "Size ";
                UnoptimizedSizeOfAppsNoResource.Text = "Orig Size ";

                NameWithRP.Text = "With Resource Packs:";
                OptimizedSizeOfAppsWithResource.Text = "Size ";
                UnoptimizedSizeOfAppsWithResource.Text = "Orig Size ";

                PopulateUI(resultNoResource.OptimizedSizeOfApps, resultNoResource.UnoptimizedSizeOfApps, resultWithResource.OptimizedSizeOfApps, resultWithResource.UnoptimizedSizeOfApps);

                ResetUI();
            }
            catch
            {
                // TBD
            }
        }

        private void PopulateUI(ulong OSANR, ulong USANR, ulong OSAWR, ulong USAWR)
        {
            OptimizedSizeOfAppsNoResource.Text += "(" + DetermineMultiplier(OSANR).ToString() + ") : " + string.Format("{0:#,#}", (OSANR / (double)(ulong)DetermineMultiplier(OSANR)).ToString("f2"));
            UnoptimizedSizeOfAppsNoResource.Text += "(" + DetermineMultiplier(USANR).ToString() + ") : " + string.Format("{0:#,#}", (USANR / (double)(ulong)DetermineMultiplier(USANR)).ToString("f2"));
            OptimizedSizeOfAppsWithResource.Text += "(" + DetermineMultiplier(OSAWR).ToString() + ") : " + string.Format("{0:#,#}", (OSAWR / (double)(ulong)DetermineMultiplier(OSAWR)).ToString("f2"));
            UnoptimizedSizeOfAppsWithResource.Text += "(" + DetermineMultiplier(USAWR).ToString() + ") : " + string.Format("{0:#,#}", (USAWR / (double)(ulong)DetermineMultiplier(USAWR)).ToString("f2"));
        }

        private multiplier DetermineMultiplier(ulong val)
        {
            if (val <= 1024)
                return multiplier.B; 

            else if (val <= (1024 * 1024))
                return multiplier.KB;

            else if (val <= (1024 * 1024 * 1024))
                return multiplier.MB;

            return multiplier.GB;

        }
        private enum multiplier
        {
            B = 1,
            KB = 1024,
            MB = 1024 * 1024,
            GB = 1024 * 1024 * 1024
        }

        private void ResetUI()
        {
            AnalyzeProgress.IsIndeterminate = false;
            _packagesToAnalyze.Clear();
            PackagesTextBox.Text = string.Empty;
            BrowseButton.IsEnabled = true;
            PackagesTextBox.ResetWaterMark();
        }

        private List<DataGridResult> PopulateComparisonGrid(AppxPackageComparisonResult result)
        {
            List<DataGridResult> dataGridResult = new List<DataGridResult>();

            foreach (string fileHash in result.CrossPackageDeDupdFiles.Keys)
            {
                if (result.CrossPackageDeDupdFiles[fileHash].Count > 1)
                {
                    DataGridResult item = new DataGridResult()
                    {
                        FileName = Path.GetFileName(result.CrossPackageDeDupdFiles[fileHash].First().FileName),
                        FileSize = string.Format("{0:#,#}", result.CrossPackageDeDupdFiles[fileHash].First().UncompressedSize),
                        FileCount = result.CrossPackageDeDupdFiles[fileHash].Count.ToString(),
                    };
                    dataGridResult.Add(item);
                }
            }

            return dataGridResult;
        }

        #endregion

        #region Browse + Select
        private void BrowseForPackages_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog o = getFiles("Select AppX Package");

            try
            {
                // grab the data
                bool? result = o.ShowDialog();

                if (result == true)
                {
                    _packagesToAnalyze = o.FileNames.ToList();

                    int cnt = 0;
                    string textBoxString = string.Empty;
                    foreach (string filePath in _packagesToAnalyze)
                    {
                        if (cnt > 0)
                            textBoxString += ", ";

                        if (cnt == 0)
                            _prevOpenFileLocation = System.IO.Path.GetDirectoryName(filePath);

                        textBoxString += System.IO.Path.GetFileName(filePath);
                        cnt++;
                    }

                    PackagesTextBox.Text = textBoxString;
                }

                AnalyzeButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ShowError("Browsing Failed", ex.Message);
            }
        }

        private OpenFileDialog getFiles(string title)
        {
            OpenFileDialog o = new OpenFileDialog();
            o.InitialDirectory = _prevOpenFileLocation == string.Empty ? @"C:\" : _prevOpenFileLocation;
            o.Multiselect = true;
            o.Filter = "appx files |*.appx; *.appxbundle; *.eappx| all files (*.*)|*.*";
            o.Title = title;

            return o;
        }

        #endregion

        #region Helpers

        private bool checkPackageLocations(List<string> packages)
        {
            if (packages.Count == 0)
            {
                ShowError("Select Packages", "Please select packages to analyze.\n");
                return false;
            }
            foreach (string package in packages)
            {
                if (!File.Exists(package))
                {
                    ShowError("Not Available", "The following package is no longer available - " + Path.GetFileName(package) + ".\n");
                    return false;
                }
            }

            return true;
        }

        private void clearState()
        {
            _packagesToAnalyze = new List<string>();
        }

        public class DataGridResult
        {
            public string FileName { get; set; }
            public string FileSize { get; set; }
            public string FileCount { get; set; }
        }
        #endregion

        #region Errors
        private delegate void ShowErrorMsgDelegate(string title, string message);

        private void ShowError(string title, string message)
        {
            this.Dispatcher.BeginInvoke(new ShowErrorMsgDelegate(ShowErrorInternal), title, message);

        }
        private void ShowErrorInternal(string title, string message)
        {
            MessageBox.Show(message, title);
            ResetUI();
        }
        #endregion

        #region Email
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start("mailto:" + e.Uri + "?subject=Comments on UWA Space Optimizations - Version " + GetRunningVersion());
        }
        #endregion
    }
}
