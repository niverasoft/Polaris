using Nivera;
using Nivera.Utils;

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using ShellProgressBar;

using Polaris.Config;
using System.Text;

namespace Polaris.Boot
{
    public static class BootLoader
    {
        public const string BaseDownloadLink = "https://cdn.nivera.tech/polaris";

        static BootLoader()
        {
            Log.JoinCategory("bootloader");
        }

        public static async Task Commence(string[] args)
        {
            Log.JoinCategory("bootloader");
            Log.Info("Hi! Loading Polaris ..");

            if (GlobalConfig.Instance.CheckPackages)
            {
                if (SystemHelper.IsLinux)
                {
                    Log.Info("Linux system detected. Checking for installed OpenJDK packages.");

                    string packages = SystemHelper.GetCommandOutput("apt list openjdk* --installed");

                    bool isInstalled = packages.Contains("openjdk-");

                    Log.Verbose(packages);

                    if (!isInstalled)
                    {
                        Log.Warn("Failed to find a compatible OpenJDK version. Installing ..");

                        packages = SystemHelper.GetCommandOutput("sudo apt install -y openjdk-17-jre");

                        if (packages.ToLower().Contains("error") || packages.ToLower().Contains("fail"))
                        {
                            Log.Error("Failed to install the openjdk-17-jre package, skipping. You will have to install it manually.");
                            Log.Error(packages);
                        }
                        else
                        {
                            Log.Info(packages);
                            Log.Info("Succesfully installed openjdk-17-jre!");
                        }
                    }

                    Log.Info("Linux system detected. Checking for installed libopus packages.");

                    Log.Verbose(SystemHelper.GetCommandOutput("sudo add-apt-repository ppa:kepstin/opus"));
                    Log.Verbose(SystemHelper.GetCommandOutput("sudo add-apt-repository ppa:chris-lea/libsodium"));
                    Log.Verbose(SystemHelper.GetCommandOutput("sudo apt update"));

                    packages = SystemHelper.GetCommandOutput("apt list *opus* --installed");

                    isInstalled = packages.Contains("libopus-dev");

                    Log.Verbose(packages);

                    if (!isInstalled)
                    {
                        Log.Warn("Failed to find a compatible libopus version. Installing ..");

                        packages = SystemHelper.GetCommandOutput("sudo apt install -y libopus-dev");

                        if (packages.ToLower().Contains("error") || packages.ToLower().Contains("fail"))
                        {
                            Log.Error("Failed to install the libopus-dev package, skipping. You will have to install it manually.");
                            Log.Error(packages);
                        }
                        else
                        {
                            Log.Info(packages);
                            Log.Info("Succesfully installed libopus-dev!");
                        }
                    }

                    Log.Info("Linux system detected. Checking for installed libsodium packages.");

                    packages = SystemHelper.GetCommandOutput("apt list *sodium* --installed");

                    isInstalled = packages.Contains("libsodium-dev");

                    Log.Verbose(packages);

                    if (!isInstalled)
                    {
                        Log.Warn("Failed to find a compatible libsodium version. Installing ..");

                        packages = SystemHelper.GetCommandOutput("sudo apt install -y libsodium-dev");

                        if (packages.ToLower().Contains("error") || packages.ToLower().Contains("fail"))
                        {
                            Log.Error("Failed to install the libsodium-dev package, skipping. You will have to install it manually.");
                            Log.Error(packages);
                        }
                        else
                        {
                            Log.Info(packages);
                            Log.Info("Succesfully installed libsodium-dev!");
                        }
                    }
                }

                if (!File.Exists($"{Directory.GetCurrentDirectory()}/Lavalink.jar"))
                {
                    Log.Error("Failed to find Lavalink.jar! Downloading latest version ..");

                    using (var web = new WebClient())
                    using (var bar = new ProgressBar(100, "Downloading Lavalink.jar .."))
                    {
                        var progress = bar.AsProgress<int>();

                        web.DownloadProgressChanged += (x, e) =>
                        {
                            progress.Report(e.ProgressPercentage);
                        };

                        await web.DownloadFileTaskAsync($"{BaseDownloadLink}/lavalink_latest.jar", $"{Directory.GetCurrentDirectory()}/Lavalink.jar");

                        bar.Dispose();
                    }

                    if (File.Exists($"{Directory.GetCurrentDirectory()}/Lavalink.jar"))
                    {
                        Log.Info("Downloaded the latest version of Lavalink.");
                    }
                    else
                    {
                        Log.Error($"Failed to download the latest version of Lavalink! You will have to download it yourself. ({BaseDownloadLink}/lavalink_latest.jar). Make sure it's name is Lavalink.jar!");
                    }
                }

                if (!File.Exists($"{Directory.GetCurrentDirectory()}/application.yml"))
                {
                    Log.Error("Lavalink requires a config to run! Generating with provided IP and password ..");

                    using (var web = new WebClient())
                    using (var bar = new ProgressBar(2, "Downloading Lavalink configuration .."))
                    {
                        web.DownloadFileCompleted += (x, e) =>
                        {
                            bar.Tick("Generating configuration file ..");
                        };

                        await web.DownloadFileTaskAsync($"{BaseDownloadLink}/lavalink_config.yml", $"{Directory.GetCurrentDirectory()}/application.yml");

                        string text = File.ReadAllText($"{Directory.GetCurrentDirectory()}/application.yml");
                        string ip = Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkAddress);
                        string passwd = Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkPassword);
                        string[] ipParts = ip.Split(':');

                        text.Replace("%IP%", ipParts[0]);
                        text.Replace("%PORT%", ipParts[1]);
                        text.Replace("%PASSWD%", passwd);

                        File.WriteAllText($"{Directory.GetCurrentDirectory()}/application.yml", text);

                        bar.Dispose();
                    }
                }
                else
                {
                    Log.Info("Lavalink configuration file found.");
                }
            }

            try
            {
                if (GlobalConfig.Instance.LavalinkAtStart)
                {
                    Log.Info("Starting Lavalink ..");

                    SystemHelper.GetCommandOutput($"screen -dmS polarisLavalink java -jar {Directory.GetCurrentDirectory()}/Lavalink.jar");

                    Log.Info("Lavalink process started on a new screen session (polarisLavalink).");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start Lavalink.");
                Log.Error(ex);
            }

            StartupArguments.Parse(args);
        }

        public static void Kill()
        {
            SystemHelper.GetCommandOutput("screen -S polarisLavalink -X quit");
        }
    }
}