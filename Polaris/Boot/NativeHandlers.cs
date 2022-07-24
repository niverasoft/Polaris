using System;
using Nivera;

namespace Polaris.Boot
{
    public static class NativeHandlers
    {
        static NativeHandlers()
        {
            Log.JoinCategory("nativehandlers");
        }

        public static void InstallHandlers()
        {
            Console.CancelKeyPress += (x, e) =>
            {
                if (e.Cancel)
                {
                    Program.Kill("Process termination requested by user.");
                }
            };

            AppDomain.CurrentDomain.ProcessExit += (x, e) =>
            {
                Program.Kill("Process termination requested by System.");
            };
        }
    }
}
