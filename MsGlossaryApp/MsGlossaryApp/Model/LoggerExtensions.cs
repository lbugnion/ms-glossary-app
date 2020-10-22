using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace MsGlossaryApp.Model
{
    public static class LoggerExtensions
    {
        private const string LogLevelVariableName = "LogLevel";
        private static LogVerbosity _logVerbosity;

        static LoggerExtensions()
        {
            var logLevelString = Environment.GetEnvironmentVariable(LogLevelVariableName);

            object logVerb;

            var success = Enum.TryParse(
                typeof(LogVerbosity), 
                logLevelString, 
                true, 
                out logVerb);

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

    public enum LogVerbosity
    {
        Normal = 0,
        Verbose = 1,
        Debug = 2
    }
}
