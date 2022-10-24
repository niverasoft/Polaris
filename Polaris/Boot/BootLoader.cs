using NiveraLib;
using NiveraLib.Logging;
using NiveraLib.Utilities;

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Polaris.Config;

namespace Polaris.Boot
{
    public static class BootLoader
    {
        private static LogId LogId = new LogId("startup / bootLoader", 101);
        private static SystemInfo system = new SystemInfo();

        public const string BaseDownloadLink = "https://cdn.nivera.tech/polaris";

        public static async Task Commence(string[] args)
        {
            Log.SendInfo($"Hello there!", LogId);
            Log.SendInfo($"Attempting to load the bot ..", LogId);

            if (GlobalConfig.Instance.CheckPackages)
            {
                if (system.IsLinux)
                {
                    Log.SendInfo("Detected a Linux-based operating system! The bot will now proceed to check for missing packages.", LogId);

                    CheckPackageByName("screen");
                    CheckPackageByName("openjdk-17-jre");
                    CheckPackageByName("ffmpeg");
                    CheckPackageByName("libopus-dev");
                    CheckPackageByName("libsodium-dev");
                }

                if (!File.Exists($"{Directory.GetCurrentDirectory()}/Lavalink.jar"))
                {
                    Log.SendWarn($"Failed to locate the Lavalink executable server, downloading ..", LogId);

                    using (var web = new WebClient())
                    {
                        web.DownloadProgressChanged += (x, e) =>
                        {
                            Log.SendInfo($"Download progress: {e.ProgressPercentage} % (received {e.BytesReceived} bytes out of {e.TotalBytesToReceive} bytes)", LogId);
                        };

                        await web.DownloadFileTaskAsync($"{BaseDownloadLink}/lavalink_latest.jar", $"{Directory.GetCurrentDirectory()}/Lavalink.jar");

                        Log.SendInfo($"Lavalink server executable was downloaded in the bot's directory.", LogId);
                    }
                }

                if (!File.Exists($"{Directory.GetCurrentDirectory()}/application.yml"))
                {
                    Log.SendWarn($"Failed to locate the Lavalink's server configuration, downloading ..", LogId);

                    using (var web = new WebClient())
                    {
                        await web.DownloadFileTaskAsync($"{BaseDownloadLink}/lavalink_config.yml", $"{Directory.GetCurrentDirectory()}/application.yml");

                        Log.SendInfo($"Configuration file downloaded, modifying password and address ..", LogId);

                        string text = File.ReadAllText($"{Directory.GetCurrentDirectory()}/application.yml");
                        string ip = Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkAddress);
                        string passwd = Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkPassword);
                        string[] ipParts = ip.Split(':');

                        text.Replace("%IP%", ipParts[0]);
                        text.Replace("%PORT%", ipParts[1]);
                        text.Replace("%PASSWD%", passwd);

                        File.WriteAllText($"{Directory.GetCurrentDirectory()}/application.yml", text);
                    }
                }
            }

            try
            {
                if (GlobalConfig.Instance.LavalinkAtStart)
                {
                    var ticks = DateTime.Now.Ticks;

                    Log.SendInfo($"Attempting to launch the Lavalink server in a new Screen Session .. (lava{ticks})", LogId);

                    LinuxCommand.ReadOutput(LinuxCommand.StartInScreen($"lava{ticks}", $"cd {Directory.GetCurrentDirectory()} && java -jar Lavalink.jar"));                 
                }
            }
            catch (Exception ex)
            {
                Log.SendException(ex, LogId);
            }

            StartupArguments.Parse(args);
        }

        public static void CheckPackageByName(string packageName)
        {
            if (!LinuxCommand.ReadOutput(LinuxCommand.ListInstalledAptPackages()).Contains(packageName))
            {
                Log.SendWarn($"Looks like a vital package is missing: {packageName}, attempting installation. You may be asked for a password.", LogId);

                var output = LinuxCommand.ReadOutput(LinuxCommand.InstallAptPackage(packageName));

                Log.SendInfo($"Attempted installation, command output:", LogId);
                Log.SendInfo(output, LogId);
            }
        }
    }
}