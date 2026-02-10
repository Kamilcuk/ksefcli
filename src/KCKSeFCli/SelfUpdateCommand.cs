using System.Runtime.InteropServices;

using CommandLine;

namespace KCKSeFCli;

[Verb("SelfUpdate", HelpText = "Updates the tool to the latest version.")]
public class SelfUpdateCommand : IGlobalCommand
{
    [Option('d', "destination", HelpText = "Save the new version to the specified path instead of replacing the current executable.")]
    public string? Destination { get; set; }

    [Option("url", HelpText = "Specify a custom URL for the update binary.")]
    public string? Url { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        string? currentExecutablePath = null;
        if (string.IsNullOrEmpty(Destination))
        {
            currentExecutablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(currentExecutablePath))
            {
                Log.LogError("Error: Could not determine the location of the current executable.");
                return 1;
            }
        }

        string downloadUrl;
        string fileName;

        if (!string.IsNullOrEmpty(Url))
        {
            downloadUrl = Url;
            fileName = Path.GetFileName(new Uri(Url).LocalPath);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            downloadUrl = "https://gitlab.com/kamcuk/kcksefcli/-/jobs/artifacts/main/raw/kcksefcli.exe?job=windows_build_main";
            fileName = "kcksefcli.exe";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            downloadUrl = "https://gitlab.com/kamcuk/kcksefcli/-/jobs/artifacts/main/raw/kcksefcli?job=linux_build_main";
            fileName = "kcksefcli";
        }
        else
        {
            Log.LogError("Error: Self-update is only supported on Windows and Linux.");
            return 1;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".exe";
        }

        string extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
        using TemporaryFile tempFile = new TemporaryFile(extension: extension);

        try
        {
            using (HttpClient httpClient = new HttpClient())
            {
                Log.LogInformation($"Downloading new version from {downloadUrl}");
                HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using (FileStream fs = new FileStream(tempFile.Path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await new Subprocess(new[] { "chmod", "+x", tempFile.Path }).CheckCallAsync(cancellationToken).ConfigureAwait(false);
            }

            string destinationPath;
            if (Destination is null)
            {
                destinationPath = currentExecutablePath!;
            }
            else
            {
                destinationPath = Directory.Exists(Destination) ? Path.Combine(Destination, fileName) : Destination;
            }

            Log.LogInformation($"Saving to {destinationPath}...");
            File.Move(tempFile.Path, destinationPath, true);
            Log.LogInformation("Update successful.");
            return 0;
        }
        catch (Exception ex)
        {
            Log.LogError($"Error during self-update: {ex.Message}");
            return 1;
        }
    }
}
