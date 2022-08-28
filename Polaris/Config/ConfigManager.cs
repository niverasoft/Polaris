using System;
using System.IO;
using System.Collections.Generic;

using Nivera.IO;
using Nivera;

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

        public static Dictionary<ulong, ServerConfig> ConfigList { get; set; } = new Dictionary<ulong, ServerConfig>();
        public static Dictionary<ulong, ServerCache> CacheList { get; set; } = new Dictionary<ulong, ServerCache>();
        public static Dictionary<ulong, VoiceCache> VoiceList { get; set; } = new Dictionary<ulong, VoiceCache>();

        public static void Load()
        {
            try
            {
                if (!File.Exists(DatabasePath))
                {
                    GlobalConfig.Instance = new GlobalConfig();
                    GlobalCache.Instance = new GlobalCache();

                    ConfigList = new Dictionary<ulong, ServerConfig>();
                    CacheList = new Dictionary<ulong, ServerCache>();
                    VoiceList = new Dictionary<ulong, VoiceCache>();

                    Save();
                }
                else
                {
                    var file = BinaryFile.ReadFrom(DatabasePath);

                    GlobalConfig.Instance = file.DeserializeFile<GlobalConfig>("globalConfig");
                    GlobalCache.Instance = file.DeserializeFile<GlobalCache>("globalCache");

                    ConfigList = file.DeserializeFile<Dictionary<ulong, ServerConfig>>("serverConfig");
                    CacheList = file.DeserializeFile<Dictionary<ulong, ServerCache>>("serverCache");
                    VoiceList = file.DeserializeFile<Dictionary<ulong, VoiceCache>>("voiceCache");
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

                GlobalConfig.Instance = new GlobalConfig();
                GlobalCache.Instance = new GlobalCache();

                ConfigList = new Dictionary<ulong, ServerConfig>();
                CacheList = new Dictionary<ulong, ServerCache>();
                VoiceList = new Dictionary<ulong, VoiceCache>();

                Save();
            }
        }

        public static void Save()
        {
            var file = new BinaryFile();

            file.SerializeFile(GlobalConfig.Instance, "globalConfig");
            file.SerializeFile(GlobalCache.Instance, "globalCache");
            file.SerializeFile(ConfigList, "serverConfig");
            file.SerializeFile(CacheList, "serverCache");
            file.SerializeFile(VoiceList, "voiceCache");
            file.WriteTo(DatabasePath);
        }
    }
}