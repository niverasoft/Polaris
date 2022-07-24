namespace Polaris.Boot
{
    public static class BootLoader
    {
        static BootLoader()
        {
            Nivera.Log.JoinCategory("boot/loader");
        }

        public static void Commence(string[] args)
        {
            Nivera.Log.Info("Hi! Loading Polaris ..");

            StartupArguments.Parse(args);
        }
    }
}