﻿using Mono.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Ionic.Zip;

namespace WinNvm
{
    internal static class NvmUtils
    {
        internal static void PrintVersion()
        {
            Console.WriteLine(
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name 
                + " " +
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
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

            if (verToInstall == "e")
            {
                throw new WinNvmException("Invalid Version " + verToInstall);
            }

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
                    {
                        throw new WinNvmException("Cannot access " + urlToDownload +
                                                  ".\nPlease check the NodeMirror property in " + Constants.RcFileName +
                                                  " in your home directory.\nAlso check you network connection");
                    }
                    throw new WinNvmException(exception.Message);
                }
                var versionJson = JsonConvert.DeserializeObject<List<NodeVersions>>(json).OrderBy(o=>o.Date).ToList();
                var tmpVersion = versionJson.Where(v => v.Version.Equals('v' + verToInstall));
                var nodeVersionses = tmpVersion as NodeVersions[] ?? tmpVersion.ToArray();

                if (!nodeVersionses.Any())
                {
                    throw new WinNvmException("Node version " + verToInstall + " is not available");
                }

                urlToDownload = GetDownloadUrl(verToInstall);
                var fileNameForSaving = GetSavePath(verToInstall);
                Console.WriteLine("Downloading :{0}",urlToDownload +" -> "+fileNameForSaving);
                webClient.DownloadFile(urlToDownload,fileNameForSaving);
                Console.WriteLine("Downloaded");

                Console.WriteLine("Extracting");
                ExtractToNvmHome(fileNameForSaving,verToInstall);
                Console.WriteLine("Folder Extracted");
            }
        }

        private static void ExtractToNvmHome(string zipFileName,string verToInstall)
        {
            using (var zipFile = ZipFile.Read(zipFileName))
            {
                zipFile.ExtractAll(Environment.GetEnvironmentVariable("NVM_HOME"));
            }

            Directory.Move(Environment.GetEnvironmentVariable("NVM_HOME")+"node-v"+verToInstall+"-win-x64"
                ,Environment.GetEnvironmentVariable("NVM_HOME")+'v'+verToInstall);
        }

        private static string GetDownloadUrl(string verToInstall)
        {
            var fileName = Environment.Is64BitOperatingSystem ? "-win-x64.zip" : "-win-x86.zip";
            return Constants.RcFileData.NodeMirror + "/v" + verToInstall + "/node-v" + verToInstall +fileName;
        }

        private static string GetSavePath(string verToInstall)
        {
            var fileName = Environment.Is64BitOperatingSystem ? "-win-x64.zip" : "-win-x86.zip";
            return Path.GetTempPath() +  "node-v" + verToInstall + fileName;
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
                    Constants.RcFileData = JsonConvert.DeserializeObject<RCFileData>(json);
                }
            }
            else
            {
                Constants.RcFileData = new RCFileData {NodeMirror = "https://nodejs.org/dist"};
            }
        }

        public static void ValidateEnvironment()
        {
            var tmpHome = Environment.GetEnvironmentVariable("NVM_HOME");
            var tmpSymLink = Environment.GetEnvironmentVariable("NVM_SYM_LINK");

            if (string.IsNullOrEmpty(tmpHome))
            {
                throw new WinNvmException("NVM_HOME is not defined please create a environment variable named NVM_HOME");
            }

            if (string.IsNullOrEmpty(tmpSymLink))
            {
                throw new WinNvmException("NVM_SYM_LINK is not defined please create a environment variable named NVM_SYM_LINK");
            }

        }
    }
}