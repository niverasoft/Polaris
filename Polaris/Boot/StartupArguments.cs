using System.Text;
using System.Linq;

using Polaris.Config;
using Polaris.Helpers;

using Nivera;

namespace Polaris.Boot
{
    public static class StartupArguments
    {
        static StartupArguments()
        {
            Log.JoinCategory("boot/args");
        }

        public static void Parse(string[] args)
        {
            if (args.Length <= 0)
            {
                Log.Info("No startup arguments detected.");
                return;
            }

            Log.Info($"Parsing startup arguments from string {string.Join(",", args)}");

            foreach (string arg in args)
            {
                if (arg.StartsWith("-token="))
                {
                    BotToken = arg.Replace("-token=", "");

                    Log.Info("Received bot token in args.");

                    GlobalConfig.Instance.Token = Encoding.UTF32.GetBytes(BotToken);

                    ConfigManager.Save();
                }

                if (arg.StartsWith("-lavaip="))
                {
                    LavalinkAddress = arg.Replace("-lavaip=", "");

                    Log.Info("Received lavalink server address in args.");

                    GlobalConfig.Instance.LavalinkAddress = Encoding.UTF32.GetBytes(LavalinkAddress);

                    ConfigManager.Save();
                }

                if (arg.StartsWith("-lavapass="))
                {
                    LavalinkPassword = arg.Replace("-lavapass=", "");

                    Log.Info("Received lavalink server password in args.");

                    GlobalConfig.Instance.LavalinkPassword = Encoding.UTF32.GetBytes(LavalinkPassword);

                    ConfigManager.Save();
                }

                if (arg.StartsWith("-globalconfig:"))
                {
                    string paramValue = StringHelpers.RemoveBeforeIndex(arg, arg.IndexOf('='));
                    string paramName = arg.Replace("-globalconfig:", "").Replace($"={paramValue}", "");

                    switch (paramName)
                    {
                        case "debug":
                            {
                                if (bool.TryParse(paramValue, out bool debug))
                                {
                                    GlobalConfig.Instance.Debug = debug;

                                    Log.Info($"Updated the value of Debug: {debug}; saving changes.");

                                    ConfigManager.Save();
                                }
                                else
                                {
                                    Log.Error($"Failed to parse {paramValue} to a boolean.");
                                }

                                break;
                            }

                        case "verbose":
                            {
                                if (bool.TryParse(paramValue, out bool verbose))
                                {
                                    GlobalConfig.Instance.Verbose = verbose;

                                    Log.Info($"Updated the value of Verbose: {verbose}; saving changes.");

                                    ConfigManager.Save();
                                }
                                else
                                {
                                    Log.Error($"Failed to parse {paramValue} to a boolean.");
                                }

                                break;
                            }

                        case "timestampformat":
                            {
                                GlobalConfig.Instance.LogTimestampFormat = paramValue;

                                Log.Info($"Updated the value of LogTimestampFormat: {paramValue}; saving changes.");

                                ConfigManager.Save();

                                break;
                            }

                        case "botownerid":
                            {
                                if (ulong.TryParse(paramValue, out ulong id))
                                {
                                    GlobalConfig.Instance.BotOwnerId = id;

                                    Log.Info($"Updated the value of BotOwnerId: {id}; saving changes.");

                                    ConfigManager.Save();
                                }
                                else
                                {
                                    Log.Error($"Failed to parse {paramValue} to an ulong.");
                                }

                                break;
                            }

                        case "botownernick":
                            {
                                GlobalConfig.Instance.BotOwnerNickname = paramValue;

                                Log.Info($"Updated the value of BotOwnerNickname: {paramValue}; saving changes.");

                                ConfigManager.Save();

                                break;
                            }

                        case "allowlava":
                            {
                                if (bool.TryParse(paramValue, out bool allow))
                                {
                                    GlobalConfig.Instance.AllowLavalink = allow;

                                    Log.Info($"Updated the value of AllowLavalink: {allow}; saving changes.");

                                    ConfigManager.Save();
                                }
                                else
                                {
                                    Log.Error($"Failed to parse {paramValue} to a boolean.");
                                }

                                break;
                            }

                        case "includesystemdebug":
                            {
                                if (bool.TryParse(paramValue, out bool include))
                                {
                                    GlobalConfig.Instance.IncludeSystemDebug = include;

                                    Log.Info($"Updated the value of IncludeSystemDebug: {include}; saving changes.");

                                    ConfigManager.Save();
                                }
                                else
                                {
                                    Log.Error($"Failed to parse {paramValue} to a boolean.");
                                }

                                break;
                            }
                    }
                }
            }
        }

        public static string BotToken { get; set; }
        public static string LavalinkAddress { get; set; }
        public static string LavalinkPassword { get; set; }
    }
}