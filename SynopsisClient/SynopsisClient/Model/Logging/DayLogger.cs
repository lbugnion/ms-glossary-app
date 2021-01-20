﻿using System;
using Microsoft.Extensions.Logging;

public class SynopsisClientLogger : ILogger
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
    private readonly SynopsisClientLoggerConfiguration _config;
    private DateTime _lastFolderDate;

    public SynopsisClientLogger(string name, SynopsisClientLoggerConfiguration config)
    {
        _name = name;
        _config = config;
    }

    public IDisposable BeginScope<TState>(TState state) => default;

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel >= _config.MinimumLogLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string color;
        string prefix;

        switch (logLevel)
        {
            case LogLevel.Trace:
                color = ConsoleCodes.FgGreen;
                prefix = "trce";
                break;

            case LogLevel.Debug:
                color = ConsoleCodes.FgGreen;
                prefix = "debg";
                break;

            case LogLevel.Warning:
                color = ConsoleCodes.BgYellow;
                prefix = "warn";
                break;

            case LogLevel.Error:
                color = ConsoleCodes.FgRed;
                prefix = "errr";
                break;

            case LogLevel.Critical:
                color = ConsoleCodes.FgRed;
                prefix = "crit";
                break;

            default:
                color = ConsoleCodes.FgBlue;
                prefix = "info";
                break;
        }

        var timestamp = DateTime.Now.ToString("yyyy:MM:dd HH:mm:ss:fff");

        Console.WriteLine($"{ConsoleCodes.BgCyan}{_name} @ {timestamp} {color}{prefix}: {state}\x1b[0m");

        //Task.Run(() =>
        //{
        //});
    }
}