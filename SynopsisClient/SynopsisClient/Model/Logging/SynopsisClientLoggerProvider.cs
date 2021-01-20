using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

public sealed class SynopsisClientLoggerProvider : ILoggerProvider
{
    private readonly SynopsisClientLoggerConfiguration _config;

    private readonly ConcurrentDictionary<string, SynopsisClientLogger> _loggers =
        new ConcurrentDictionary<string, SynopsisClientLogger>();

    public SynopsisClientLoggerProvider(SynopsisClientLoggerConfiguration config)
    {
        _config = config;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(
            categoryName, 
            name => new SynopsisClientLogger(name, _config));
    }

    public void Dispose() => _loggers.Clear();
}