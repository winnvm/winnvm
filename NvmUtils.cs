using Mono.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;

namespace WinNvm
{
    class NvmUtils
    {

        internal static void PrintVersion()
        {
            Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
        }

        internal static void ShowHelp(OptionSet options)
        {
            Console.WriteLine();
            Console.WriteLine("Usage: winnvm [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }

        internal static void validateNodeVersion(string verToInstall)
        {
            if (verToInstall == "e")
            {
                throw new WinNvmException("Invalid Version " + verToInstall);
            }

            using (var webClient = new WebClient())
            {
                List<NodeVersions> versionJson;
                var json = webClient.DownloadString("http://repository.emirates.com/repository/raw-nodejs-org/dist/index.json");
                versionJson = JsonConvert.DeserializeObject<List<NodeVersions>>(json);
                foreach (var v in versionJson)
                {
                    Console.WriteLine(v.version);
                }
            }

        }
    }
}
