using System;
using System.Threading.Tasks;
using Mono.Options;

namespace WinNvm
{
    internal static class Program
    {
        private enum CommandAction
        {
            None,
            Install,
            List,
            Use,
            Remove,
            Help,
            Version
        }

        public static async Task<int> Main(string[] args)
        {
            try
            {
                var rcData = NvmUtils.LoadRcJson();

                var action = CommandAction.None;
                string? targetVersion = null;

                void SetAction(CommandAction next, string? version = null)
                {
                    if (action != CommandAction.None)
                    {
                        throw new WinNvmException("Specify only one command option at a time.");
                    }

                    action = next;
                    targetVersion = version;
                }

                var options = new OptionSet
                {
                    {
                        "i|install=",
                        "To install a new version of NodeJS",
                        ver => SetAction(CommandAction.Install, ver)
                    },
                    {
                        "l|list",
                        "To list all installed versions of NodeJS",
                        _ => SetAction(CommandAction.List)
                    },
                    {
                        "u|use=",
                        "To use the given version of NodeJS",
                        ver => SetAction(CommandAction.Use, ver)
                    },
                    {
                        "r|remove=",
                        "To uninstall a version of NodeJS",
                        ver => SetAction(CommandAction.Remove, ver)
                    },
                    {
                        "h|help",
                        "Show this message",
                        _ => SetAction(CommandAction.Help)
                    },
                    {
                        "v|version",
                        "Display Version",
                        _ => SetAction(CommandAction.Version)
                    }
                };

                var extra = options.Parse(args);
                if (extra.Count > 0)
                {
                    throw new WinNvmException($"Invalid option(s): {string.Join(" ", extra)}");
                }

                switch (action)
                {
                    case CommandAction.None:
                    case CommandAction.Help:
                        NvmUtils.ShowHelp();
                        return 0;

                    case CommandAction.Version:
                        NvmUtils.PrintVersion();
                        return 0;

                    case CommandAction.Install:
                    {
                        var env = NvmUtils.ValidateEnvironment();
                        await NvmUtils.ValidateNodeVersionAndDownloadAsync(targetVersion!, rcData, env);
                        return 0;
                    }

                    case CommandAction.List:
                    {
                        var env = NvmUtils.ValidateEnvironment();
                        NvmUtils.PrintListOfInstalledNodeVersions(env);
                        return 0;
                    }

                    case CommandAction.Use:
                    {
                        var env = NvmUtils.ValidateEnvironment();
                        NvmUtils.ValidateNodeVersionAndUse(targetVersion!, env);
                        return 0;
                    }

                    case CommandAction.Remove:
                    {
                        var env = NvmUtils.ValidateEnvironment();
                        NvmUtils.UninstallNodeJs(targetVersion!, env);
                        return 0;
                    }

                    default:
                        NvmUtils.ShowHelp();
                        return 0;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERR: {ex.Message}");
                if (ex is OptionException || ex is WinNvmException)
                {
                    Console.Error.WriteLine("Try 'winnvm --help' for more information.");
                }
                return 2;
            }
        }
    }
}
