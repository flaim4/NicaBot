using Serilog;

namespace SubBot.Utils;

public static class Logger
{
    public static void Init()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public static void Info(string message) => Log.Information(message);
    public static void Warn(string message) => Log.Warning(message);
    public static void Error(string message) => Log.Error(message);
    public static void Debug(string message) => Log.Debug(message);
}
