using System;
using System.IO;
using System.Collections.Generic;

using NiveraLib.Logging;
using NiveraLib.IO.Binary;
using Newtonsoft.Json;
using NiveraLib;

namespace Polaris.Config
{
    public static class ConfigManager
    {
        public static readonly LogId logId = LogIdGenerator.GenerateId("configManager");

        public static string DatabasePath { get => $"{Directory.GetCurrentDirectory()}/config.txt"; }

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
                    string[] lines = File.ReadAllLines(DatabasePath);

                    GlobalConfig.Instance = JsonConvert.DeserializeObject<GlobalConfig>(lines[0]);
                    GlobalCache.Instance = JsonConvert.DeserializeObject<GlobalCache>(lines[1]);

                    ConfigList = JsonConvert.DeserializeObject<Dictionary<ulong, ServerConfig>>(lines[2]);
                    CacheList = JsonConvert.DeserializeObject<Dictionary<ulong, ServerCache>>(lines[3]);
                    VoiceList = JsonConvert.DeserializeObject<Dictionary<ulong, VoiceCache>>(lines[4]);
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
            try
            {
                string[] lines = new string[5];

                lock (ConfigList)
                {
                    lock (CacheList)
                    {
                        lock (VoiceList)
                        {
                            lines[0] = JsonConvert.SerializeObject(GlobalConfig.Instance);
                            lines[1] = JsonConvert.SerializeObject(GlobalCache.Instance);
                            lines[2] = JsonConvert.SerializeObject(new Dictionary<ulong, ServerConfig>(ConfigList));
                            lines[3] = JsonConvert.SerializeObject(new Dictionary<ulong, ServerCache>(CacheList));
                            lines[4] = JsonConvert.SerializeObject(new Dictionary<ulong, VoiceCache>(VoiceList));
                        }
                    }
                }

                File.WriteAllLines(DatabasePath, lines);
            }
            catch (Exception ex)
            {
                Log.SendException(ex, logId);
            }
        }
    }
}