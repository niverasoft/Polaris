using System;
using System.Collections.Generic;
using System.Text;

namespace Polaris.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        string LoadMessage { get; }
        string Version { get; }
        string Author { get; }

        IEventHandler EventHandler { get; }


        void OnEnabled();
        void OnDisabled();
        void OnReloaded();

        bool CheckVersion();
    }
}
