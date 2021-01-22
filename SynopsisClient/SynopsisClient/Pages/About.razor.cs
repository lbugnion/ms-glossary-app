using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace SynopsisClient.Pages
{
    public partial class About
    {
        private const string VersionKey = "Version";
        private const string VersionDateKey = "VersionDate";

        public string ClientVersion
        {
            get;
            private set;
        }

        public string AssemblyVersion
        {
            get;
            private set;
        }

        public string ReleaseDate
        {
            get;
            private set;
        }

        protected override void OnInitialized()
        {
            Log.LogInformation("-> OnInitialized");

            // Check build number

            var versionInConfig = Config.GetValue<string>(VersionKey);

            if (!string.IsNullOrEmpty(versionInConfig))
            {
                ClientVersion = versionInConfig;
            }
            else
            {
                try
                {
                    var version = Assembly
                        .GetExecutingAssembly()
                        .GetName()
                        .Version;

                    Log.LogDebug($"Full version: {version}");
                    ClientVersion = $"V{version.ToString(4)}";
                    Log.LogDebug($"clientVersion: {ClientVersion}");
                }
                catch
                {
                    Log.LogWarning($"Assembly not found");
                    ClientVersion = "N/A";
                }
            }
        }
    }
}
