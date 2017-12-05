using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Ionic.Zip;
using Mono.Options;
using Newtonsoft.Json;

namespace WinNvm
{
    internal static class NvmUtils
    {
        internal static void PrintVersion()
        {
            Console.WriteLine(
                Assembly.GetExecutingAssembly().GetName().Name
                + " " +
                Assembly.GetExecutingAssembly().GetName().Version
            );
        }

        internal static void ShowHelp(OptionSet options)
        {
            Console.WriteLine();
            Console.WriteLine("Usage: winnvm [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
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
                var nodeVersionses = tmpVersion as NodeVersions[] ?? tmpVersion.ToArray();

                if (!nodeVersionses.Any())
                    throw new WinNvmException("Node version " + verToInstall + " is not available");

                urlToDownload = GetDownloadUrl(verToInstall);
                var fileNameForSaving = GetSavePath(verToInstall);
                Console.WriteLine("Downloading :{0} -> {1}", urlToDownload, fileNameForSaving);
                webClient.DownloadFile(urlToDownload, fileNameForSaving);
                Console.WriteLine("Downloaded");

                Console.WriteLine("Extracting");
                ExtractToNvmHome(fileNameForSaving, verToInstall);
                Console.WriteLine("Folder Extracted");
            }
        }

        internal static void LoadRcJson()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var rcFile = homePath + Path.DirectorySeparatorChar + Constants.RcFileName;

            if (File.Exists(rcFile))
                using (var r = new StreamReader(rcFile))
                {
                    var json = r.ReadToEnd();
                    Constants.RcFileData = JsonConvert.DeserializeObject<RCFileData>(json);
                }
            else
                Constants.RcFileData = new RCFileData {NodeMirror = "https://nodejs.org/dist"};
        }

        private static void ExtractToNvmHome(string zipFileName, string verToInstall)
        {
            using (var zipFile = ZipFile.Read(zipFileName))
            {
                var appPath = Environment.GetEnvironmentVariable(Constants.NvmHomeVarName) + "v" + verToInstall;
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
            return Constants.RcFileData.NodeMirror + "/v" + verToInstall + "/node-v" + verToInstall + GetFileName();
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

        internal static void ValidateEnvironment()
        {
            Constants.NvmHome = Environment.GetEnvironmentVariable(Constants.NvmHomeVarName);
            Constants.NvmSymLink = Environment.GetEnvironmentVariable(Constants.NvmSymLinkVarName);

            if (string.IsNullOrEmpty(Constants.NvmHome))
                throw new WinNvmException(Constants.NvmHomeVarName +
                                          " is not defined please create a environment variable named " +
                                          Constants.NvmHomeVarName);

            if (string.IsNullOrEmpty(Constants.NvmSymLink))
                throw new WinNvmException(Constants.NvmSymLinkVarName +
                                          " is not defined please create a environment variable named " +
                                          Constants.NvmSymLinkVarName);
        }

        public static void ValidateNodeVersionAndUse(string verToUse)
        {

            var src = Environment.GetEnvironmentVariable(Constants.NvmHomeVarName) + 'v' + verToUse;
            var dest = Environment.GetEnvironmentVariable(Constants.NvmSymLinkVarName);

            if (!Directory.Exists(src))
            {
                throw new WinNvmException("Version "+verToUse+ " is not installed.");
            }

            if (Directory.Exists(dest))
            {
                Directory.Delete(dest);
            }

            var cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe"); ;
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
    }
}