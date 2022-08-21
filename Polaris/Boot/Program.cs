using System;
using System.Threading.Tasks;

using Nivera;

using Polaris.Properties;
using Polaris.Config;
using Polaris.Discord;
using Polaris.Plugins;

namespace Polaris.Boot
{
    public static class Program
    {
        static Program()
        {
            Log.JoinCategory("main");
        }

        public static void Kill(string message = null)
        {
            if (message == null)
                message = "Reason Unknown";

            GlobalConfig.Instance.NativeExit = true;
            ConfigManager.Save();

            Log.Warn($"Exiting: {message}");

            TaskSystems.Kill();
            DiscordNetworkHandlers.Kill();

            Environment.Exit(0);
        }

        public static async Task Main(string[] args)
        {
            try
            {
                LoadNivera();

                try
                {
                    BuildInfo.Retrieve();
                    TaskSystems.Restart();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    Console.ReadLine();
                }

                try
                {
                    ConfigManager.Load();

                    DebugConfigs();

                    if (!GlobalConfig.Instance.NativeExit)
                    {
                        Log.Error("Polaris did not exit natively on previous shutdown. Please, do not close Polaris through the task manager etc., you may lose your saved data!");
                        Log.Error("Ignore this if it is your first startup.");
                    }

                    GlobalConfig.Instance.NativeExit = false;

                    ConfigManager.Save();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                    Console.ReadLine();
                }

                NativeHandlers.InstallHandlers();

                Log.Info("Starting up ..");
                Log.Verbose($"System: {BuildInfo.Version} on {Environment.OSVersion.VersionString} [{Environment.OSVersion.Platform}]");

                BootLoader.Commence(args);

                Log.Info("Welcome to Polaris!");

                Console.WriteLine(Resources.Logo);

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    Log.Warn("Polaris is not completely compatible with Unix based systems! You should use the Windows version if you have the option, otherwise you have to expect some issues.");

                Log.Info("Thank you for using Polaris!");

                await DiscordNetworkHandlers.InstallAsync();
                PluginManager.Enable();
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        public static void DebugConfigs()
        {
            if (GlobalConfig.Instance.Debug)
            {
                Log.Arguments(GlobalConfig.Instance);
            }
        }

        public static void LoadNivera()
        {
            LibConfig libConfig = new LibConfig(null, true, true, false, true);

            libConfig.UseSystemLogger();

            LibProperties.Load(libConfig);
        }
    }
}