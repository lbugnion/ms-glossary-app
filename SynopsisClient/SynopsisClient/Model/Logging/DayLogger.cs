using System;
using System.Drawing;
using Microsoft.Extensions.Logging;

public class DayLogger : ILogger
{
    private static class ConsoleCodes
    {
        public const string Reset = "\x1b[0m";
        public const string Bright = "\x1b[1m";
        public const string Dim = "\x1b[2m";
        public const string Underscore = "\x1b[4m";
        public const string Blink = "\x1b[5m";
        public const string Reverse = "\x1b[7m";
        public const string Hidden = "\x1b[8m";

        public const string FgBlack = "\x1b[30m";
        public const string FgRed = "\x1b[31m";
        public const string FgGreen = "\x1b[32m";
        public const string FgYellow = "\x1b[33m";
        public const string FgBlue = "\x1b[34m";
        public const string FgMagenta = "\x1b[35m";
        public const string FgCyan = "\x1b[36m";
        public const string FgWhite = "\x1b[37m";

        public const string BgBlack = "\x1b[40m";
        public const string BgRed = "\x1b[41m";
        public const string BgGreen = "\x1b[42m";
        public const string BgYellow = "\x1b[43m";
        public const string BgBlue = "\x1b[44m";
        public const string BgMagenta = "\x1b[45m";
        public const string BgCyan = "\x1b[46m";
        public const string BgWhite = "\x1b[47m";
    }

    private readonly string _name;
    private readonly DayLoggerConfiguration _config;
    private DateTime _lastFolderDate;

    public DayLogger(
        string name,
        DayLoggerConfiguration config)
    {
        (_name, _config) = (name, config);
    }

    public IDisposable BeginScope<TState>(TState state) => default;

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel == _config.LogLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        Console.WriteLine("DAYLOGGER ---------------");
        //Console.WriteLine($"eventId {eventId}");
        //Console.WriteLine($"TState {typeof(TState)}");

        if (_config.LogLevel > logLevel)
        {
            Console.WriteLine("Disabled: " + logLevel);
            return;
        }

        string color;

        switch (logLevel)
        {
            case LogLevel.Trace:
                color = ConsoleCodes.FgCyan;
                break;

            case LogLevel.Debug:
                color = ConsoleCodes.FgBlue;
                break;

            case LogLevel.Error:
                color = ConsoleCodes.FgRed;
                break;

            case LogLevel.Critical:
                color = ConsoleCodes.BgRed;
                break;

            default:
                color = ConsoleCodes.FgBlack;
                break;
        }

        Console.WriteLine($"{logLevel}: {color}{state}\x1b[0m");

        //Task.Run(() =>
        //{
        //});
    }
}