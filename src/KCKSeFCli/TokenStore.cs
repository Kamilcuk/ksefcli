using System.Text;
using System.Text.Json;

using KSeF.Client.Core.Models.Authorization;

namespace KCKSeFCli;

public class TokenStore
{
    public record Data
    {
        public AuthenticationOperationStatusResponse Response { get; init; }
        public Data(AuthenticationOperationStatusResponse Response)
        {
            if (Response is null) throw new Exception("Response is null");
            if (Response.AccessToken is null) throw new Exception("Response.AccessToken is null");
            if (Response.RefreshToken is null) throw new Exception("Response.RefreshToken is null");
            this.Response = Response;
        }
    }

    public record Key(string Nazwa, ProfileConfig Profile)
    {
        public string ToCacheKey()
        {
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                string profileJson = System.Text.Json.JsonSerializer.Serialize(Profile);
                string nip = Profile.Nip;
                string environment = Profile.Environment;
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(profileJson);
                byte[] hash = sha256.ComputeHash(bytes);
                string hashString = Convert.ToHexString(hash).ToLower();
                return $"{Nazwa}_{environment}_{nip}_{hashString}";
            }
        }
    }

    private readonly string _path;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public TokenStore(string path)
    {
        _path = Environment.ExpandEnvironmentVariables(path);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        Log.LogInformation($"Token store loaded from: {_path}");
    }

    private Dictionary<string, Data> LoadTokens(LockedFileStream lockFile)
    {
        if (lockFile.Fs.Length == 0)
        {
            Dictionary<string, Data> empty = new Dictionary<string, Data>();
            byte[] emptyData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(empty, _jsonOptions));
            lockFile.Fs.Write(emptyData, 0, emptyData.Length);
            lockFile.Fs.Flush(true);
            return empty;
        }
        lockFile.Fs.Seek(0, SeekOrigin.Begin);
        byte[] data = new byte[lockFile.Fs.Length];
        lockFile.Fs.ReadExactly(data);
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, Data>>(data, _jsonOptions) ?? new Dictionary<string, Data>();
        }
        catch (Exception e) when (e is JsonException || e is Exception)
        {
            Log.LogWarning($"Invalid JSON in token cache file: {_path}. Overwriting with empty data.");
            lockFile.Fs.Seek(0, SeekOrigin.Begin);
            lockFile.Fs.SetLength(0);
            Dictionary<string, Data> empty = new Dictionary<string, Data>();
            byte[] emptyData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(empty, _jsonOptions));
            lockFile.Fs.Write(emptyData, 0, emptyData.Length);
            lockFile.Fs.Flush(true);
            return empty;
        }
    }

    public Data? GetToken(Key key)
    {
        using (LockedFileStream lockFile = new LockedFileStream(_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            Dictionary<string, Data> tokens = LoadTokens(lockFile);
            if (tokens.TryGetValue(key.ToCacheKey(), out Data? token))
            {
                string invalidReason = token?.Response is null ? "Response is null" :
                                       token.Response.RefreshToken is null ? "RefreshToken is null" :
                                       token.Response.AccessToken is null ? "AccessToken is null" : "";
                if (!string.IsNullOrEmpty(invalidReason))
                {
                    Log.LogWarning($"Invalid token data found in cache for key: {key.ToCacheKey()} (reason: {invalidReason}). Deleting the entry.");
                    tokens.Remove(key.ToCacheKey());
                    lockFile.Fs.Seek(0, SeekOrigin.Begin);
                    lockFile.Fs.SetLength(0);
                    byte[] newData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tokens, _jsonOptions));
                    lockFile.Fs.Write(newData, 0, newData.Length);
                    lockFile.Fs.Flush(true);
                    return null;
                }
                return token;
            }
            return null;
        }
    }

    public void SetToken(Key key, Data token)
    {
        using (LockedFileStream lockFile = new LockedFileStream(_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            Dictionary<string, Data> tokens = LoadTokens(lockFile);
            tokens[key.ToCacheKey()] = token;
            lockFile.Fs.Seek(0, SeekOrigin.Begin);
            lockFile.Fs.SetLength(0);
            byte[] newData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tokens, _jsonOptions));
            lockFile.Fs.Write(newData, 0, newData.Length);
            lockFile.Fs.Flush(true);
        }
    }

    public bool RemoveToken(Key key)
    {
        using (LockedFileStream lockFile = new LockedFileStream(_path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            Dictionary<string, Data> tokens = LoadTokens(lockFile);
            if (tokens.Remove(key.ToCacheKey()))
            {
                lockFile.Fs.Seek(0, SeekOrigin.Begin);
                lockFile.Fs.SetLength(0);
                byte[] newData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tokens, _jsonOptions));
                lockFile.Fs.Write(newData, 0, newData.Length);
                lockFile.Fs.Flush(true);
                return true;
            }
            return false;
        }
    }
}
