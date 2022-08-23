using Nivera;
using Nivera.Utils;

using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;

using ShellProgressBar;

using Polaris.Config;
using System.Text;

namespace Polaris.Boot
{
    public static class BootLoader
    {
        public const string BaseDownloadLink = "https://cdn.nivera.tech/polaris";

        public static Process LavalinkProcess;

        static BootLoader()
        {
            Log.JoinCategory("bootloader");
        }

        public static async Task Commence(string[] args)
        {
            Log.Info("Hi! Loading Polaris ..");

            if (SystemHelper.IsLinux)
            {
                Log.Info("Linux system detected. Checking for installed Java packages.");

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
            }

            if (!File.Exists($"./Lavalink.jar"))
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

                if (File.Exists("./Lavalink.jar"))
                {
                    Log.Info("Downloaded the latest version of Lavalink.");
                }
                else
                {
                    Log.Error($"Failed to download the latest version of Lavalink! You will have to download it yourself. ({BaseDownloadLink}/lavalink_latest.jar). Make sure it's name is Lavalink.jar!");
                }
            }

            if (!File.Exists("./application.yml"))
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

                    string text = File.ReadAllText("./application.yml");
                    string ip = Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkAddress);
                    string passwd = Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkPassword);
                    string[] ipParts = ip.Split(':');

                    text.Replace("%IP%", ipParts[0]);
                    text.Replace("%PORT%", ipParts[1]);
                    text.Replace("%PASSWD%", passwd);

                    File.WriteAllText("./application.yml", text);

                    bar.Dispose();
                }
            }
            else
            {
                Log.Info("Lavalink configuration file found.");
            }

            try
            {
                Log.Info("Starting Lavalink ..");

                LavalinkProcess = SystemHelper.GetLinuxCommand($"java -jar {Directory.GetCurrentDirectory()}/Lavalink.jar");
                LavalinkProcess.Exited += (x, e) =>
                {
                    Log.Warn("The Lavalink process has exited.");
                };

                LavalinkProcess.Start();

                Log.Info("Lavalink process started.");
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
            if (LavalinkProcess == null)
                return;

            LavalinkProcess.Kill();
            LavalinkProcess.Dispose();
        }
    }
}