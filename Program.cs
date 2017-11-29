using Mono.Options;
using System;
using System.Collections.Generic;

namespace WinNvm
{
    internal class Program
    {
        
        public static void Main(string[] args)
        {

            var shouldShowHelp = false;
            var verToInstall = "";
            var isInstall = false;
            var isUse = false;
            var verToUse = "";
            var showVersion = false;
            var logLevel = "WARN";

            // thses are the available options, not that they set the variables
            var options = new OptionSet {
                {
                    "i|install <version>",
                    "To install a new version of NodeJS",
                    ver => { verToInstall = ver; isInstall=true; }
                },
                {
                    "u|use <version>",
                    "To use the given version of NodeJS",
                    ver => { verToUse = ver; isUse=true; }
                },
                {
                    "h|help",
                    "Show this message",
                    h => shouldShowHelp = h != null
                },
                {
                    "l|log <level>",
                    "Default level is 'WARN'. Options: ['INFO', 'DEBUG', 'WARN', 'ERROR']",
                    l => logLevel = l
                },
                {
                    "v|version",
                    "Display Version",
                    version => showVersion = true
                }
            };


            List<string> extra = null;
            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'winnvm --help' for more information.");
                Environment.Exit(2);
            }

            if (showVersion)
            {
                PrintVersion();
            }

            if(isInstall && isUse)
            {
                Console.WriteLine("Cannot use install and use same time");
                ShowHelp(options);
                Environment.Exit(505);
            }

            if (shouldShowHelp || extra == null || extra.Count < 1)
            {
                ShowHelp(options);
                Environment.Exit(0);
            }

        }

        private static void PrintVersion()
        {
            Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name+" " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
        }

        private static void ShowHelp(OptionSet options)
        {
            Console.WriteLine();
            Console.WriteLine("Usage: winnvm [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }
    }
}