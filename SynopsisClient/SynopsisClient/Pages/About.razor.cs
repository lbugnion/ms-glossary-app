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

            var versionStringInConfig = Config.GetValue<string>(VersionKey);

            Log.LogDebug($"versionStringInConfig: {versionStringInConfig}");

            if (string.IsNullOrEmpty(versionStringInConfig))
            {
                ClientVersion = "N/A";
            }
            else
            {
                var version = new Version(versionStringInConfig);
                Log.LogDebug($"version: {version}");

                ClientVersion = $"V{version}";
            }

            var dateInConfig = Config.GetValue<string>(VersionDateKey);

            Log.LogDebug($"dateInConfig: {dateInConfig}");

            if (string.IsNullOrEmpty(dateInConfig))
            {
                ReleaseDate = "N/A";
            }
            else
            {
                DateTime releaseDate;
                var success = DateTime.TryParseExact(
                    dateInConfig, 
                    "yyyyMMdd", 
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out releaseDate);
                
                if (success)
                {
                    ReleaseDate = releaseDate.ToShortDateString();
                }
                else
                {
                    Log.LogTrace("Unable to parse release date");
                }
            }

            Log.LogInformation("OnInitialized ->");
        }
    }
}
