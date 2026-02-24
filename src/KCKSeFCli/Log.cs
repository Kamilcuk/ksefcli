using Microsoft.Extensions.Logging;

public static class Log {
    private static ILoggerFactory? _loggerFactory;

    public static ILogger Logger { get; private set; } = default!;

    public static void ConfigureLogging(bool verbose = false, bool quiet = false) {
        _loggerFactory = LoggerFactory.Create(builder => {
            LogLevel kcksefCliLevel = LogLevel.Information;
            LogLevel microsoftLevel = LogLevel.Warning;
            LogLevel systemLevel = LogLevel.Warning;

            if (verbose) {
                kcksefCliLevel = LogLevel.Debug;
                microsoftLevel = LogLevel.Debug;
                systemLevel = LogLevel.Debug;
            }

            if (quiet) {
                kcksefCliLevel = LogLevel.Warning;
            }

            builder.AddFilter("KCKSeFCli", kcksefCliLevel)
                   .AddFilter("Microsoft", microsoftLevel)
                   .AddFilter("System", systemLevel)
                   .AddConsole(options => {
                       options.LogToStandardErrorThreshold = LogLevel.Trace;
                   })
                   .AddSimpleConsole(options => {
                       options.SingleLine = true;
                       options.TimestampFormat = "HH:mm:ss ";
                   });
        });

        Logger = _loggerFactory.CreateLogger("KCKSeFCli");
    }

    public static void Trace(string message) => Logger.LogTrace(message);
    public static void Debug(string message) => Logger.LogDebug(message);
    public static void Information(string message) => Logger.LogInformation(message);
    public static void Warning(string message) => Logger.LogWarning(message);
    public static void Error(string message) => Logger.LogError(message);
    public static void Critical(string message) => Logger.LogCritical(message);
}
