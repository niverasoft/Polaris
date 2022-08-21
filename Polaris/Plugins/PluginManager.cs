using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;

using Nivera;
using Nivera.Reflection;

using Polaris.Properties;
using Polaris.Config;
using Polaris.Discord;

namespace Polaris.Plugins
{
    public static class PluginManager
    {
        public static List<IPlugin> Plugins = new List<IPlugin>();
        public static string Path = "./Plugins/";

        static PluginManager()
        {
            Log.JoinCategory("plugins");
        }

        public static void Enable()
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);

            foreach (var file in Directory.GetFiles(Path, "*.dll"))
            {
                Assembly assembly = Assembly.Load(File.ReadAllBytes(file));
                Type type = assembly?.GetTypes().FirstOrDefault(x => x.IsAssignableFrom(typeof(IPlugin)) || x.IsSubclassOf(typeof(IPlugin)));

                if (type != null)
                {
                    IPlugin plugin = ReflectUtils.Instantiate(type) as IPlugin;

                    if (plugin != null)
                    {
                        if (!plugin.CheckVersion())
                            continue;

                        plugin.OnEnabled();
                        plugin.EventHandler?.OnLoaded();

                        Log.Info($"Loaded {plugin.Name} ({plugin.Version}) by {plugin.Author}");

                        if (!string.IsNullOrEmpty(plugin.Description))
                            Log.Info(plugin.Description);
                        if (!string.IsNullOrEmpty(plugin.LoadMessage))
                            Log.Info(plugin.LoadMessage);

                        Plugins.Add(plugin);
                    }
                }
            }
        }
    }
}