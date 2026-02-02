using System.Runtime.InteropServices;
using CommandLine;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace KSeFCli;

[Verb("SelfUpdate", HelpText = "Updates the tool to the latest version.")]
public class SelfUpdateCommand : IGlobalCommand
{
    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        string currentExecutablePath = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrEmpty(currentExecutablePath))
        {
            Console.Error.WriteLine("Error: Could not determine the location of the current executable.");
            return 1;
        }

        string downloadUrl;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            downloadUrl = "https://gitlab.com/kamcuk/ksefcli/-/jobs/artifacts/main/raw/ksefcli.exe?job=windows_build_main";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            downloadUrl = "https://gitlab.com/kamcuk/ksefcli/-/jobs/artifacts/main/raw/ksefcli?job=linux_build_main";
        }
        else
        {
            Console.Error.WriteLine("Error: Self-update is only supported on Windows and Linux.");
            return 1;
        }

        string extension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;
        using var tempFile = new TemporaryFile(extension: extension);

        try
        {
            using (var httpClient = new HttpClient())
            {
                Console.WriteLine($"Downloading new version from {downloadUrl}...");
                var response = await httpClient.GetAsync(downloadUrl, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                using (var fs = new FileStream(tempFile.Path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await new Subprocess(new[] { "chmod", "+x", tempFile.Path }).CheckCallAsync(cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine("Replacing the current executable...");
            File.Move(tempFile.Path, currentExecutablePath, true);
            Console.WriteLine("Update successful.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during self-update: {ex.Message}");
            return 1;
        }
    }
}
