namespace Polaris.Boot
{
    public static class BootLoader
    {
        static BootLoader()
        {
            Nivera.Log.JoinCategory("bootloader");
        }

        public static void Commence(string[] args)
        {
            Nivera.Log.Info("Hi! Loading Polaris ..");

            StartupArguments.Parse(args);
        }
    }
}