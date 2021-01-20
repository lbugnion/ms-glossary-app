using System;
using Microsoft.Extensions.Logging;

public class DayLoggerConfiguration
{
    public int EventId { get; set; }

    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}