using System;
using System.IO;
using System.Collections.Generic;

using Nivera.IO;
using Nivera;

using Polaris.Boot;

namespace Polaris.Config
{
    public static class ConfigManager
    {
        static ConfigManager()
        {
            Log.JoinCategory("configs");
        }

        public static string DatabasePath { get => $"./config"; }

        public static string Announcement;
        public static bool HasAnnouncement;

        public static Dictionary<ulong, ServerConfig> ConfigList { get; set; }

        public static void Load()
        {
            try
            {
                if (!File.Exists(DatabasePath))
                {
                    GlobalConfig.Instance = new GlobalConfig();
                    ConfigList = new Dictionary<ulong, ServerConfig>();

                    Save();
                }
                else
                {
                    var file = BinaryFile.ReadFrom(DatabasePath);

                    GlobalConfig.Instance = file.DeserializeFile<GlobalConfig>("globalConfig");
                    ConfigList = file.DeserializeFile<Dictionary<ulong, ServerConfig>>("serverConfig");
                }

                if (File.Exists("./announcement.txt"))
                {
                    Announcement = File.ReadAllText("./announcement.txt");
                    HasAnnouncement = !string.IsNullOrEmpty(Announcement);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        public static void Save()
        {
            var file = new BinaryFile();

            file.SerializeFile(GlobalConfig.Instance, "globalConfig");
            file.SerializeFile(ConfigList, "serverConfig");
            file.WriteTo(DatabasePath);

            Log.Info("Configuration saved succesfully.");
        }
    }
}