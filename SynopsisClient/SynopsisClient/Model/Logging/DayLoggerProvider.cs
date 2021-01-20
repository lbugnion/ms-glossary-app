using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

public sealed class DayLoggerProvider : ILoggerProvider
{
    private readonly DayLoggerConfiguration _config;
    private readonly ConcurrentDictionary<string, DayLogger> _loggers =
        new ConcurrentDictionary<string, DayLogger>();

    public DayLoggerProvider(DayLoggerConfiguration config) =>
        _config = config;

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new DayLogger(name, _config));

    public void Dispose() => _loggers.Clear();
}