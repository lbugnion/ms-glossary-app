using Microsoft.Extensions.Logging;
using System;

namespace MsGlossaryApp.Model
{
    public enum LogVerbosity
    {
        Normal = 0,
        Verbose = 1,
        Debug = 2
    }

    public static class LoggerExtensions
    {
        private const string LogVerbosityVariableName = "LogVerbosity";
        private static LogVerbosity _logVerbosity;

        static LoggerExtensions()
        {
            var logLevelString = Environment.GetEnvironmentVariable(LogVerbosityVariableName);

            var success = Enum.TryParse(
                typeof(LogVerbosity),
                logLevelString,
                true,
                out object logVerb);

            if (success)
            {
                _logVerbosity = (LogVerbosity)logVerb;
            }
            else
            {
                _logVerbosity = LogVerbosity.Normal;
            }
        }

        public static void LogInformationEx(
            this ILogger log,
            string message,
            LogVerbosity verbosity,
            params object[] args)
        {
            if (_logVerbosity >= verbosity)
            {
                log.LogInformation(message, args);
            }
        }
    }
}