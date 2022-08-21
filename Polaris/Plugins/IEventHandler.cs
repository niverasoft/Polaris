using System;
using System.Collections.Generic;
using System.Text;

namespace Polaris.Plugins
{
    public interface IEventHandler
    {
        void OnLoaded();
        void OnUnloaded();
    }
}
