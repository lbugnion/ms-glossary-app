namespace Microsoft.Extensions.Logging
{
    public static class LoggerExtensions
    {
        public static void LogTrace(this ILogger logger, string message)
        {
            logger.LogEx(LogLevel.Trace, message);
        }

        public static void LogDebug(this ILogger logger, string message)
        {
            logger.LogEx(LogLevel.Debug, message);
        }

        public static void LogInformation(this ILogger logger, string message)
        {
            logger.LogEx(LogLevel.Information, message);
        }

        public static void LogWarning(this ILogger logger, string message)
        {
            logger.LogEx(LogLevel.Warning, message);
        }

        public static void LogError(this ILogger logger, string message)
        {
            logger.LogEx(LogLevel.Error, message);
        }

        public static void LogCritical(this ILogger logger, string message)
        {
            logger.LogEx(LogLevel.Critical, message);
        }

        public static void LogEx(this ILogger logger, LogLevel level, string message)
        {
            // Temporarily map Trace and Debug to Information, with a prefix.
            // Also respect LogLevel set in the configuration.

            if (logger.IsEnabled(level))
            {
                switch (level)
                {
                    case LogLevel.Trace:
                        logger.LogInformation($"TRACE: {message}");
                        break;

                    case LogLevel.Debug:
                        logger.LogInformation($"DEBUG: {message}");
                        break;

                    default:
                        logger.Log(level, message);
                        break;
                }
            }
        }
    }
}
