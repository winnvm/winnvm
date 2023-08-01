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

            // these are the available options, not that they set the variables
            var options = new OptionSet
            {
                {
                    "i|install=",
                    "To install a new version of NodeJS",
                    ver => 
                    {
                        try
                        {
                            NvmUtils.ValidateNodeVersionAndDownload(ver);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("ERR: " + e.Message);
                            Environment.Exit(2);
                        }
                    }
                },
                {
                    "u|use=",
                    "To use the given version of NodeJS",
                    ver =>
                    {
                        try
                        {
                            NvmUtils.ValidateNodeVersionAndUse(ver);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("ERR: " + e.Message);
                            Environment.Exit(2);
                        }
                    }
                },
                {
                    "r|remove=",
                    "To uninstall a version of NodeJS",
                    ver =>
                    {
                        try
                        {
                            NvmUtils.UninstallNodeJs(ver);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("ERR: " + e.Message);
                            Environment.Exit(2);
                        }
                    }
                },
                {
                    "h|help",
                    "Show this message",
                    h =>
                    {
                        NvmUtils.ShowHelp();
                        Environment.Exit(0);
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