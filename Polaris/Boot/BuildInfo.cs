using System;

using Polaris.Enums;
using Polaris.Properties;

namespace Polaris.Boot
{
    public static class BuildInfo
    {
        public static BotBranchType Branch { get; set; }

        public static Version Version { get; set; }

        public static string VersionString { get; set; }

        public static int VersionMajor { get; set; }
        public static int VersionMinor { get; set; }
        public static int VersionBuild { get; set; }

        public static char ReleaseLetter { get; set; }

        public static void Retrieve()
        {
            VersionString = Resources.Version;

            string[] versArgs = VersionString.Split('-');

            VersionMajor = int.Parse(versArgs[0]);
            VersionMinor = int.Parse(versArgs[1]);
            VersionBuild = int.Parse(versArgs[2]);
            ReleaseLetter = versArgs[3][0];

            switch (versArgs[4])
            {
                case "REL":
                    Branch = BotBranchType.Release;
                    break;
                case "BET":
                    Branch = BotBranchType.Beta;
                    break;
                case "TES":
                    Branch = BotBranchType.Testing;
                    break;
                default:
                    Branch = BotBranchType.Testing;
                    break;
            }

            Version = new Version(VersionMajor, VersionMinor, VersionBuild);
        }
    }
}