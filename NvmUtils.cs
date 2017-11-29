using Mono.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace WinNvm
{
    internal static class NvmUtils
    {
        internal static void PrintVersion()
        {
            Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " " +
                              System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
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
                var versionJson = JsonConvert.DeserializeObject<List<NodeVersions>>(json);
                var tmpVersion = versionJson.Where(v => v.Version.Equals('v' + verToInstall));

                if (!tmpVersion.Any())
                {
                    throw new WinNvmException("Node version " + verToInstall + " is not available");
                }

                urlToDownload = Constants.RcFileData.NodeMirror + "/v" + verToInstall + "/node-v" + verToInstall +
                                "-win-x64.zip";

                webClient.DownloadFile(urlToDownload,"/Users/karthik/Dev/Temp"+Path.DirectorySeparatorChar+"node-v" + verToInstall +
                "-win-x64.zip");
            }
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
    }
}