using System.Runtime.InteropServices;

using CommandLine;

namespace KCKSeFCli;

public abstract class IGlobalCommand
{
    public static readonly string CacheDir = GetCacheDir();
    public static readonly string ConfigDir = GetConfigDir();

    private static string GetCacheDir()
    {
        string cacheDir;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            cacheDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Caches");
        }
        else // Linux and other Unix-like systems
        {
            string? xdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            if (!string.IsNullOrEmpty(xdgCacheHome))
            {
                cacheDir = xdgCacheHome;
            }
            else
            {
                cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
            }
        }
        return Path.Combine(cacheDir, "kcksefcli");
    }

    private static string GetConfigDir()
    {
        string configDir;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            configDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
        }
        else // Linux and other Unix-like systems
        {
            string? xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdgConfigHome))
            {
                configDir = xdgConfigHome;
            }
            else
            {
                configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            }
        }
        return Path.Combine(configDir, "kcksefcli");
    }

    [Option('v', "verbose", HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }

    [Option('q', "quiet", HelpText = "Enable quiet mode (warnings and errors only)")]
    public bool Quiet { get; set; }

    public void ConfigureLogging() => Log.ConfigureLogging(Verbose, Quiet);

    public abstract Task<int> ExecuteAsync(CancellationToken cancellationToken);
}