using System.Text;
using System.Linq;

using Polaris.Config;
using Polaris.Helpers;

using NiveraLib;
using NiveraLib.Logging;

namespace Polaris.Boot
{
    public static class StartupArguments
    {
        private static LogId logId = new LogId("boot / args", 103);

        public static void Parse(string[] args)
        {
            if (args.Length <= 0)
            {
                Log.SendInfo("No startup arguments detected.", logId);

                return;
            }

            foreach (string arg in args)
            {
                if (arg.StartsWith("-token="))
                {
                    BotToken = arg.Replace("-token=", "");

                    GlobalConfig.Instance.Token = Encoding.UTF32.GetBytes(BotToken);

                    ConfigManager.Save();
                }

                if (arg.StartsWith("-lavaip="))
                {
                    LavalinkAddress = arg.Replace("-lavaip=", "");

                    GlobalConfig.Instance.LavalinkAddress = Encoding.UTF32.GetBytes(LavalinkAddress);

                    ConfigManager.Save();
                }

                if (arg.StartsWith("-lavapass="))
                {
                    LavalinkPassword = arg.Replace("-lavapass=", "");

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
                                    ConfigManager.Save();
                                }

                                break;
                            }

                        case "verbose":
                            {
                                if (bool.TryParse(paramValue, out bool verbose))
                                {
                                    GlobalConfig.Instance.Verbose = verbose;
                                    ConfigManager.Save();
                                }

                                break;
                            }

                        case "timestampformat":
                            {
                                GlobalConfig.Instance.LogTimestampFormat = paramValue;
                                ConfigManager.Save();

                                break;
                            }

                        case "botownerid":
                            {
                                if (ulong.TryParse(paramValue, out ulong id))
                                {
                                    GlobalConfig.Instance.BotOwnerId = id;
                                    ConfigManager.Save();
                                }

                                break;
                            }

                        case "botownernick":
                            {
                                GlobalConfig.Instance.BotOwnerNickname = paramValue;
                                ConfigManager.Save();

                                break;
                            }

                        case "allowlava":
                            {
                                if (bool.TryParse(paramValue, out bool allow))
                                {
                                    GlobalConfig.Instance.AllowLavalink = allow;
                                    ConfigManager.Save();
                                }

                                break;
                            }

                        case "allowdiscordlog":
                            {
                                if (bool.TryParse(paramValue, out bool allow))
                                {
                                    GlobalConfig.Instance.AllowDiscordLogOutput = allow;
                                    ConfigManager.Save();
                                }

                                break;
                            }

                        case "discordlogid":
                            {
                                if (ulong.TryParse(paramValue, out ulong id))
                                {
                                    GlobalConfig.Instance.DiscordLogOutputChannelId = id;
                                    ConfigManager.Save();
                                }

                                break;
                            }

                        case "allowlavalinkstart":
                            {
                                if (bool.TryParse(paramValue, out bool allow))
                                {
                                    GlobalConfig.Instance.LavalinkAtStart = allow;
                                    ConfigManager.Save();
                                }

                                break;
                            }

                        case "checkdependencypackages":
                            {
                                if (bool.TryParse(paramValue, out bool check))
                                {
                                    GlobalConfig.Instance.CheckPackages = check;
                                    ConfigManager.Save();
                                }

                                break;
                            }

                        case "spotifyclientid":
                            {
                                GlobalConfig.Instance.SpotifyClientId = paramValue;
                                ConfigManager.Save();

                                break;
                            }

                        case "spotifyclientsecret":
                            {
                                GlobalConfig.Instance.SpotifyClientSecret = paramValue;
                                ConfigManager.Save();

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