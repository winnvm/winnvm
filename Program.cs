using System;
using Mono.Options;

namespace WinNvm
{
    internal static class Program
    {
        public static void Main(string[] args)
        {

            try
            {
                NvmUtils.LoadRcJson();
                NvmUtils.ValidateEnvironment();
            }
            catch (Exception exception)
            {
                Console.WriteLine("ERR: " + exception.Message);
                Environment.Exit(2);
            }

            var nodeJsVersion = string.Empty;
            var toInstall = false;
            var toUse = false;
            var toUninstall = false;
            var envName = string.Empty;
            var showHelp = false;
            // These are the available options, not that they set the variables
            var options = new OptionSet
            {
                {
                    "i|install=",
                    "To install a new version of NodeJS",
                    ver =>
                    {
                        nodeJsVersion = ver;
                        toInstall = true;
                    }
                },
                {
                    "u|use=",
                    "To use the given version of NodeJS",
                    ver =>
                    {
                        nodeJsVersion = ver;
                        toUse = true;
                    }
                },
                {
                    "r|remove=",
                    "To uninstall a version of NodeJS",
                    ver =>
                    {
                        nodeJsVersion = ver;
                        toUninstall = true;
                    }
                },
                {
                    "n|name=",
                    "Name of the environment.",
                    name =>
                    {
                        envName = name;
                    }
                },
                {
                    "h|help",
                    "Show this message",
                    h =>
                    {
                        showHelp = true;
                    }
                },
                {
                    "v|version",
                    "Display Version",
                    version => NvmUtils.PrintVersion()
                }
            };

            try
            {
                var extra = options.Parse(args);
                if (extra != null && extra.Count > 0)
                {
                    throw new WinNvmException("Invalid options");
                }

                if (toInstall && toUninstall)
                {
                    throw new WinNvmException(@"{0} and {1} Options cannot be used together","Install","Uninstall");
                }

                if (showHelp)
                {
                    Console.WriteLine("Usage: winnvm <options>");
                    options.WriteOptionDescriptions(Console.Out);
                }

                if (toInstall)
                {
                    NvmUtils.ValidateNodeVersionAndDownload(nodeJsVersion,envName);
                }

                if (toUse)
                {
                    NvmUtils.ValidateNodeVersionAndUse(nodeJsVersion);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("ERR: Try 'winnvm --help' for more information.");
                Environment.Exit(2);
            }
            Environment.Exit(0);
        }
    }
}