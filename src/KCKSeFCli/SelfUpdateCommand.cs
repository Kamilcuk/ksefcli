using System.Runtime.InteropServices;

using CommandLine;

namespace KCKSeFCli;

[Verb("SelfUpdate", HelpText = "Updates the tool to the latest version.")]
public class SelfUpdateCommand : IGlobalCommand {
    [Option('d', "destination", HelpText = "Save the new version to the specified path instead of replacing the current executable.")]
    public string? Destination { get; set; }

    [Option("url", HelpText = "Specify a custom URL for the update binary.")]
    public string? Url { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        string? currentExecutablePath = null;
        if (string.IsNullOrEmpty(Destination)) {
            currentExecutablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(currentExecutablePath)) {
                Log.Error("Error: Could not determine the location of the current executable.");
                return 1;
            }
        }

        string downloadUrl;
        string fileName;

        if (!string.IsNullOrEmpty(Url)) {
            downloadUrl = Url!;
            fileName = Path.GetFileName(new Uri(Url!).LocalPath);
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            downloadUrl = "https://gitlab.com/kamcuk/kcksefcli/-/jobs/artifacts/main/raw/kcksefcli.exe?job=windows_build_main";
            fileName = "kcksefcli.exe";
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            downloadUrl = "https://gitlab.com/kamcuk/kcksefcli/-/jobs/artifacts/main/raw/kcksefcli?job=linux_build_main";
            fileName = "kcksefcli";
        } else {
            Log.Error("Error: Self-update is only supported on Windows and Linux.");
            return 1;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) {
            fileName += ".exe";
        }

        string extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
        using TemporaryFile tempFile = new TemporaryFile(extension: extension);

        try {
            using (HttpClient httpClient = new HttpClient()) {
                Log.Information($"Downloading new version from {downloadUrl}");
                HttpResponseMessage response = await httpClient.GetAsync(downloadUrl, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using (FileStream fs = new FileStream(tempFile.Path, FileMode.Create, FileAccess.Write, FileShare.None)) {
                    response.Content.CopyToAsync(fs).Wait();
                }
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                new Subprocess(new[] { "chmod", "+x", tempFile.Path }).CheckCallAsync(cancellationToken).Wait();
            }

            string destinationPath;
            if (Destination is null) {
                destinationPath = currentExecutablePath!;
            } else {
                destinationPath = Directory.Exists(Destination) ? Path.Combine(Destination, fileName) : Destination;
            }

            Log.Information($"Saving to {destinationPath}...");
            if (File.Exists(destinationPath)) File.Delete(destinationPath);
            File.Move(tempFile.Path, destinationPath);
            Log.Information("Update successful.");
            return 0;
        } catch (Exception ex) {
            Log.Error($"Error during self-update: {ex.Message}");
            return 1;
        }
    }
}
