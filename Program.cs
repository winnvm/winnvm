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
            var isInstall = false;
            var isUse = false;
            var verToUse = "";
            var showVersion = false;

            // thses are the available options, not that they set the variables
            var options = new OptionSet {
                {
                    "i|install",
                    "To install a new version of NodeJS",
                    ver => { isInstall=true; }
                },
                {
                    "u|use <version>",
                    "To use the given version of NodeJS",
                    ver => { isUse=true; }
                },
                {
                    "h|help",
                    "Show this message",
                    h => shouldShowHelp = h != null
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
                NvmUtils.PrintVersion();
            }

            if(isInstall && isUse)
            {
                Console.WriteLine("Cannot use install and use same time");
                NvmUtils.ShowHelp(options);
                Environment.Exit(505);
            }

            if (shouldShowHelp || extra == null || extra.Count < 1)
            {
                NvmUtils.ShowHelp(options);
                Environment.Exit(0);
            }

            if (isInstall)
            {
                verToUse = extra[0];
                try
                {
                    NvmUtils.validateNodeVersion(verToUse);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Environment.Exit(2);
                }
            }
        }

    }
}