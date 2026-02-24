namespace KCKSeFCli;

public static class Downloader {
    private static readonly HttpClient HttpClient = new();

    public static async Task DownloadFileWithTimestampCheckAsync(string url, string destinationPath, CancellationToken cancellationToken) {
        try {
            DateTimeOffset? remoteLastModified = null;
            try {
                using HttpRequestMessage headRequest = new(HttpMethod.Head, url);
                HttpResponseMessage headResponse = await HttpClient.SendAsync(headRequest, cancellationToken).ConfigureAwait(false);
                headResponse.EnsureSuccessStatusCode();
                remoteLastModified = headResponse.Content.Headers.LastModified;
            } catch (HttpRequestException e) {
                Log.Warning($"Could not get {url} metadata: {e.Message}");
            }

            if (File.Exists(destinationPath) && remoteLastModified.HasValue && File.GetLastWriteTimeUtc(destinationPath) >= remoteLastModified.Value.UtcDateTime) {
                Log.Information($"File {Path.GetFileName(destinationPath)} is up to date with {url}");
                return;
            }

            Log.Information($"Downloading file from {url} to {destinationPath}");
            byte[] fileBytes = await HttpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
            await File.WriteAllBytesAsync(destinationPath, fileBytes, cancellationToken).ConfigureAwait(false);
            Log.Information($"Downloaded file from {url} to {destinationPath}");

            if (remoteLastModified.HasValue) {
                File.SetLastWriteTimeUtc(destinationPath, remoteLastModified.Value.UtcDateTime);
            }
        } catch (HttpRequestException e) {
            Log.Warning($"Failed to download file from {url} to {destinationPath}: {e.Message}");
            if (!File.Exists(destinationPath)) {
                throw new Exception($"File could not be downloaded from {url} and does not exist in cache at {destinationPath}", e);
            }
        }
    }
}
