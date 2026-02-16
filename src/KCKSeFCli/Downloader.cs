namespace KCKSeFCli;

public static class Downloader
{
    private static readonly HttpClient HttpClient = new();

    public static async Task DownloadFileWithTimestampCheckAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        try
        {
            DateTimeOffset? remoteLastModified = null;
            try
            {
                using HttpRequestMessage headRequest = new(HttpMethod.Head, url);
                HttpResponseMessage headResponse = await HttpClient.SendAsync(headRequest, cancellationToken).ConfigureAwait(false);
                headResponse.EnsureSuccessStatusCode();
                remoteLastModified = headResponse.Content.Headers.LastModified;
            }
            catch (HttpRequestException e)
            {
                Log.LogWarning($"Could not get remote file metadata: {e.Message}");
            }

            if (File.Exists(destinationPath) && remoteLastModified.HasValue && File.GetLastWriteTimeUtc(destinationPath) >= remoteLastModified.Value.UtcDateTime)
            {
                Log.LogInformation($"File '{Path.GetFileName(destinationPath)}' is up to date.");
                return;
            }

            Log.LogInformation($"Downloading file from {url}...");
            byte[] fileBytes = await HttpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(destinationPath, fileBytes, cancellationToken).ConfigureAwait(false);
            Log.LogInformation($"Downloaded file to {destinationPath}");

            if (remoteLastModified.HasValue)
            {
                File.SetLastWriteTimeUtc(destinationPath, remoteLastModified.Value.UtcDateTime);
            }
        }
        catch (HttpRequestException e)
        {
            Log.LogWarning($"Failed to download file: {e.Message}");
            if (!File.Exists(destinationPath))
            {
                throw new Exception($"File could not be downloaded and does not exist in cache at {destinationPath}", e);
            }
        }
    }
}
