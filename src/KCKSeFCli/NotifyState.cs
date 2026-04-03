using System.Text;
using System.Text.Json;

namespace KCKSeFCli;

public class NotifyState {
    public Dictionary<string, ProfileNotifyState> Profiles { get; set; } = new();

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public static NotifyState Load(string path) {
        if (!File.Exists(path)) {
            return new NotifyState();
        }
        try {
            using (LockedFileStream lockFile = new LockedFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                lockFile.Fs.Seek(0, SeekOrigin.Begin);
                byte[] data = new byte[lockFile.Fs.Length];
                lockFile.Fs.ReadExactly(data, 0, data.Length);
                return JsonSerializer.Deserialize<NotifyState>(data, _jsonOptions) ?? new NotifyState();
            }
        } catch (Exception ex) {
            Log.Error($"Failed to load notify state from {path}: {ex.Message}");
            return new NotifyState();
        }
    }

    public void Save(string path) {
        string dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        using (LockedFileStream lockFile = new LockedFileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) {
            byte[] newData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, _jsonOptions));
            lockFile.Fs.Seek(0, SeekOrigin.Begin);
            lockFile.Fs.SetLength(0);
            lockFile.Fs.Write(newData, 0, newData.Length);
            lockFile.Fs.Flush(true);
        }
    }
}

public class ProfileNotifyState {
    public DateTimeOffset? LastInvoicingDate { get; set; }
    public string? LastKsefNumber { get; set; }
}
