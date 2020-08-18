using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Ionic.Zip;
using Newtonsoft.Json;

namespace WinNvm
{
    internal static class NvmUtils
    {

        private static void ExtractToNvmHome(string zipFileName, string verToInstall)
        {
            using (var zipFile = ZipFile.Read(zipFileName))
            {
                var appPath = Constants.NvmHome + "v" + verToInstall;
                zipFile.ToList().ForEach(entry =>
                {
                    if (entry.FileName.StartsWith("node-v" + verToInstall + GetFileNameWithoutZip()))
                        entry.FileName =
                            entry.FileName.Substring(("node-v" + verToInstall + GetFileNameWithoutZip()).Length);
                    Console.WriteLine(entry.FileName);
                    entry.Extract(appPath, ExtractExistingFileAction.OverwriteSilently);
                });
            }
        }

        private static string GetDownloadUrl(string verToInstall)
        {
            return Constants.RcFileData.NodeMirror + "v" + verToInstall + "/node-v" + verToInstall + GetFileName();
        }

        private static string GetShaSumUrl(string verToInstall)
        {
            return Constants.RcFileData.NodeMirror + "v" + verToInstall + "/SHASUMS256.txt";
        }
        
        private static string GetSavePath(string verToInstall)
        {
            return Path.GetTempPath() + "v" + verToInstall + ".zip";
        }

        private static string GetFileName()
        {
            return Environment.Is64BitOperatingSystem ? "-win-x64.zip" : "-win-x86.zip";
        }

        private static string GetFileNameWithoutZip()
        {
            return Environment.Is64BitOperatingSystem ? "-win-x64" : "-win-x86";
        }
        
        internal static void PrintVersion()
        {
            Console.WriteLine(
                Assembly.GetExecutingAssembly().GetName().Name
                + " " +
                Assembly.GetExecutingAssembly().GetName().Version
            );
        }

        internal static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: winnvm [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine(@"
    -i, --install <version>    To install a new version of NodeJS
    -u, --use <version>        To use the given version of NodeJS
    -r, --remove <version>     To uninstall a version of NodeJS
    -h, --help                 Show this message
    -v, --version              Display Version");
        }

        internal static void LoadRcJson()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var rcFile = homePath + Path.DirectorySeparatorChar + Constants.RcFileName;

            if (File.Exists(rcFile))
            {
                using (var r = new StreamReader(rcFile))
                {
                    var json = r.ReadToEnd();
                    Constants.RcFileData = JsonConvert.DeserializeObject<RcFileData>(json);
                }
            }

            if (Constants.RcFileData == null || string.IsNullOrEmpty(Constants.RcFileData.NodeMirror))
            {
                Constants.RcFileData = new RcFileData {NodeMirror = "https://nodejs.org/dist/"};
            }

            if (!Constants.RcFileData.NodeMirror.EndsWith("/"))
            {
                Constants.RcFileData.NodeMirror = Constants.RcFileData.NodeMirror + "/";
            }
        }
        
        private static string GetChecksumFile(string fileName)
        {
            var fileStream = new FileStream(fileName, FileMode.OpenOrCreate,
                FileAccess.Read);
            
            using (var bufferedStream = new BufferedStream(fileStream, 1024 * 32))
            {
                var sha = new SHA256Managed();
                var checksum = sha.ComputeHash(bufferedStream);
                return BitConverter.ToString(checksum).Replace("-", string.Empty);
            }
        }
        
        private static void ValidateSha256Sum(string fileNameForSaving, string shaInfo)
        {
            if (shaInfo == null) throw new ArgumentNullException(nameof(shaInfo));
            var shaSumForFile = GetChecksumFile(fileNameForSaving);
            if (!shaInfo.ToUpper().Equals(shaSumForFile.ToUpper()))
            {
                throw new WinNvmException("SHA256 Checksum failed");
            }
        }


        internal static void ValidateNodeVersionAndDownload(string verToInstall)
        {
            var urlToDownload = Constants.RcFileData.NodeMirror + "/index.json";

            using (var webClient = new WebClient())
            {
                string json;
                try
                {
                    json = webClient.DownloadString(urlToDownload);
                }
                catch (WebException exception)
                {
                    if (((HttpWebResponse) exception.Response).StatusCode == HttpStatusCode.NotFound)
                        throw new WinNvmException("Cannot access " + urlToDownload +
                                                  ".\nPlease check the NodeMirror property in " + Constants.RcFileName +
                                                  " in your home directory.\nAlso check you network connection");
                    throw new WinNvmException(exception.Message);
                }
                var versionJson = JsonConvert.DeserializeObject<List<NodeVersions>>(json).OrderBy(o => o.Date).ToList();
                var tmpVersion = versionJson.Where(v => v.Version.Equals('v' + verToInstall));
                var nodeVersions = tmpVersion as NodeVersions[] ?? tmpVersion.ToArray();

                if (!nodeVersions.Any())
                {
                    throw new WinNvmException("Node version " + verToInstall + " is not available");
                }

                urlToDownload = GetDownloadUrl(verToInstall);
                
                var shaSums =  webClient.DownloadString(GetShaSumUrl(verToInstall)).Split(Environment.NewLine.ToCharArray());
                var theLine = shaSums.Single(line => line.EndsWith("node-v" + verToInstall + GetFileName()));
                var shaInfo = Regex.Split(theLine, @"\s+")[0];
                var fileNameForSaving = GetSavePath(verToInstall);
                Console.WriteLine("Downloading :{0} -> {1}", urlToDownload, fileNameForSaving);
                webClient.DownloadFile(urlToDownload, fileNameForSaving);
                Console.WriteLine("Downloaded");

                ValidateSha256Sum(fileNameForSaving, shaInfo);

                Console.WriteLine("Extracting");
                ExtractToNvmHome(fileNameForSaving, verToInstall);
                Console.WriteLine("Folder Extracted");
            }
        }

        internal static void ValidateEnvironment()
        {
            Constants.NvmHome = Environment.GetEnvironmentVariable(Constants.NvmHomeVarName);
            Constants.NvmSymLink = Environment.GetEnvironmentVariable(Constants.NvmSymLinkVarName);

            if (string.IsNullOrEmpty(Constants.NvmHome))
            {
                throw new WinNvmException(Constants.NvmHomeVarName +
                                          " is not defined please create a environment variable named " +
                                          Constants.NvmHomeVarName);
            }

            if (string.IsNullOrEmpty(Constants.NvmSymLink))
            {
                throw new WinNvmException(Constants.NvmSymLinkVarName +
                                          " is not defined please create a environment variable named " +
                                          Constants.NvmSymLinkVarName);
            }

        if (!Constants.NvmHome.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                Constants.NvmHome = Constants.NvmHome + Path.DirectorySeparatorChar;
            }
            if (!Constants.NvmSymLink.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                Constants.NvmSymLink = Constants.NvmSymLink + Path.DirectorySeparatorChar;
            }
        }

        internal static void ValidateNodeVersionAndUse(string verToUse)
        {
            var src = Constants.NvmHome + 'v' + verToUse;
            var dest = Constants.NvmSymLink;

            if (!Directory.Exists(src))
            {
                throw new WinNvmException("Version " + verToUse + " is not installed.");
            }

            if (Directory.Exists(dest))
            {
                Directory.Delete(dest);
            }

            var cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");

            Console.WriteLine("{0} {1}", cmd, "/c mklink /J " + dest + " " + src);

            var psInfo = new ProcessStartInfo(cmd, "/c mklink /J " + dest + " " + src)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var getIpInfo = Process.Start(psInfo);

            if (getIpInfo == null) return;
            var myOutput = getIpInfo.StandardOutput;
            getIpInfo.WaitForExit(3000);
            if (getIpInfo.HasExited)
            {
                Console.WriteLine(myOutput.ReadToEnd());
            }
        }

        internal static void UninstallNodeJs(string verToUse)
        {
            var src = Constants.NvmHome + 'v' + verToUse;
            if (Directory.Exists(src))
            {
                Directory.Delete(src,true);
                Console.WriteLine(verToUse+" is uninstalled");
            }
            else
            {
                throw new WinNvmException("Version "+verToUse+" not installed");
            }
        }
    }
}