using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Polaris.Properties;
using Polaris.Config;
using Polaris.Discord;
using Polaris.Logging;

using NiveraLib;
using NiveraLib.Timers;
using NiveraLib.Versioning;
using NiveraLib.Logging;
using NiveraLib.Logging.Features;
using Polaris.Helpers.Music;

namespace Polaris.Boot
{
    public static class Program
    {
        public static LogId LogID = new LogId("boot / core", 100);

        public static double UptimeSeconds { get; set; }
        public static Timer UptimeCounter { get; set; }
        public static DiscordLogger DiscordLogger { get; set; }

        public static NiveraLib.Versioning.Version Version { get; set; }

        public static void Kill(string message = null)
        {
            GlobalConfig.Instance.NativeExit = true;
            ConfigManager.Save();

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
                    string[] versArgs = Resources.Version.Split('.');

                    Version = NiveraLib.Versioning.Version.Get(
                        int.Parse(versArgs[0]),
                        int.Parse(versArgs[1]),
                        int.Parse(versArgs[2]),
                        versArgs[3][0],
                        versArgs[4] == "release" ? Release.ProductionRelease : Release.DevelopmentRelease);

                    TaskSystems.Restart();
                }
                catch (Exception ex)
                {
                    Log.SendException(ex);

                    Console.ReadLine();
                }

                Log.SendInfo($"Welcome! Now loading version: {Version}.{Version.Release.Name}", LogID);

                try
                {
                    Log.SendInfo($"Loading your bot configuration ..");

                    ConfigManager.Load();

                    if (!GlobalConfig.Instance.NativeExit)
                        Log.SendError($"Please refrain from sending a SIGABRT to this process. We cannot save your config.", LogID);

                    GlobalConfig.Instance.NativeExit = false;
                    ConfigManager.Save();
                }
                catch (Exception ex)
                {
                    Log.SendException(ex, LogID);

                    Console.ReadLine();
                }

                NativeHandlers.InstallHandlers();

                await BootLoader.Commence(args);

                Log.SendRaw(Resources.Logo, ConsoleColor.Green);

                UptimeCounter = new Timer("PolarisUptimeCounter", false, 1000, (x, y) =>
                {
                    UptimeSeconds += y;
                });

                UptimeCounter.Start();

                await DiscordNetworkHandlers.InstallAsync();

                MusicSearch.Load();

                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        public static void LoadNivera()
        {
            LibProperties.InstallLogger(
                new LogControllerConfig()
                .ResetColorConfig()
                .ResetLogLevel()
                .AddLogLevel(MessageLevel.Output)
                .AddLogLevel(MessageLevel.Debug)
                .AddLogLevel(MessageLevel.JsonObject)
                .AddLogLevel(MessageLevel.Trace)
                .AddLogLevel(MessageLevel.Verbose), new List<ILogger>() { (DiscordLogger = new DiscordLogger()), new ConsoleLogger() });

            LibProperties.Load(new LibConfig(false, false));
        }
    }
}