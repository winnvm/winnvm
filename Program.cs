using System;
using System.Threading.Tasks;

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

                var (action, targetVersion) = ParseArgs(args);

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
                if (ex is WinNvmException)
                {
                    Console.Error.WriteLine("Try 'winnvm --help' for more information.");
                }
                return 2;
            }
        }

        private static (CommandAction Action, string? TargetVersion) ParseArgs(string[] args)
        {
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

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var (name, inlineValue) = SplitOption(arg);

                string ReadRequiredValue()
                {
                    if (inlineValue is not null)
                    {
                        if (inlineValue.Length == 0)
                        {
                            throw new WinNvmException($"Missing required value for option '{arg}'.");
                        }

                        return inlineValue;
                    }

                    if (i + 1 >= args.Length || string.IsNullOrEmpty(args[i + 1]))
                    {
                        throw new WinNvmException($"Missing required value for option '{arg}'.");
                    }

                    return args[++i];
                }

                void EnsureNoValue()
                {
                    if (inlineValue is not null)
                    {
                        throw new WinNvmException($"Option '{arg}' does not take a value.");
                    }
                }

                switch (name)
                {
                    case "i":
                    case "install":
                        SetAction(CommandAction.Install, ReadRequiredValue());
                        break;

                    case "l":
                    case "list":
                        EnsureNoValue();
                        SetAction(CommandAction.List);
                        break;

                    case "u":
                    case "use":
                        SetAction(CommandAction.Use, ReadRequiredValue());
                        break;

                    case "r":
                    case "remove":
                        SetAction(CommandAction.Remove, ReadRequiredValue());
                        break;

                    case "h":
                    case "help":
                        EnsureNoValue();
                        SetAction(CommandAction.Help);
                        break;

                    case "v":
                    case "version":
                        EnsureNoValue();
                        SetAction(CommandAction.Version);
                        break;

                    default:
                        throw new WinNvmException($"Invalid option(s): {arg}");
                }
            }

            return (action, targetVersion);
        }

        private static (string Name, string? InlineValue) SplitOption(string arg)
        {
            string body;
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                body = arg[2..];
            }
            else if (arg.StartsWith('-'))
            {
                body = arg[1..];
                if (body.Length > 1 && body[0] is 'i' or 'u' or 'r' && body[1] is not ('=' or ':'))
                {
                    return (body[..1], body[1..]);
                }
            }
            else if (arg.StartsWith('/'))
            {
                body = arg[1..];
            }
            else
            {
                throw new WinNvmException($"Invalid option(s): {arg}");
            }

            if (body.Length == 0)
            {
                throw new WinNvmException($"Invalid option(s): {arg}");
            }

            var sepIndex = body.IndexOfAny(['=', ':']);
            return sepIndex >= 0
                ? (body[..sepIndex], body[(sepIndex + 1)..])
                : (body, null);
        }
    }
}
