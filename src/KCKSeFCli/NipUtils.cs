using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using KSeF.Client.Extensions;
namespace KCKSeFCli;
public static class NipUtils {
    private static readonly Regex _nipRegex = new Regex(@".*\|nip-(\d+)\|.*", RegexOptions.Compiled);
    public static string? GetNipFromCertificate(string? certificateContent) {
        string? subject = GetCertificateSubject(certificateContent);
        if (subject == null) return null;
        if (subject.Contains("organizationIdentifier=VATPL-", StringComparison.OrdinalIgnoreCase)) {
            int orgIdIndex = subject.IndexOf("organizationIdentifier=VATPL-", StringComparison.OrdinalIgnoreCase);
            int nipStart = orgIdIndex + "organizationIdentifier=VATPL-".Length;
            int commaIndex = subject.IndexOf(',', nipStart);
            return (commaIndex == -1 ? subject.Substring(nipStart) : subject.Substring(nipStart, commaIndex - nipStart)).Trim();
        }
        return null;
    }
    public static string? GetCertificateSubject(string? certificateContent) {
        if (string.IsNullOrWhiteSpace(certificateContent)) return null;
        try {
            byte[] certBytes = Encoding.UTF8.GetBytes(certificateContent);
            using X509Certificate2 cert = certBytes.LoadCertificate();
            return cert.Subject;
        } catch {
        }
        return null;
    }
    public static string ExtractNipFromToken(string token) {
        Match match = _nipRegex.Match(token);
        if (!match.Success) throw new InvalidOperationException("Token does not contain NIP.");
        string nip = match.Groups[1].Value;
        if (string.IsNullOrEmpty(nip)) throw new InvalidOperationException("Token does not contain NIP.");
        AssertNipIsValid(nip);
        return nip;
    }
    public static bool MatchNip(string? nip1, string? nip2) {
        if (string.IsNullOrWhiteSpace(nip1) || string.IsNullOrWhiteSpace(nip2)) return false;
        return NormalizeNip(nip1) == NormalizeNip(nip2);
    }
    public static string NormalizeNip(string nip) {
        return new string(nip.Where(char.IsDigit).ToArray());
    }
    public static void AssertNipIsValid(string? nip) {
        if (string.IsNullOrEmpty(nip)) throw new ArgumentException("NIP cannot be null or empty.");
        string cleanNip = NormalizeNip(nip);
        if (cleanNip.Length != 10) throw new ArgumentException($"Invalid NIP length: {cleanNip}. NIP must be 10 digits long.");
        if (!long.TryParse(cleanNip, out _)) throw new ArgumentException($"Invalid NIP format: {cleanNip}. NIP must be a number.");
        int[] weights = { 6, 5, 7, 2, 3, 4, 5, 6, 7 };
        int sum = 0;
        for (int i = 0; i < 9; i++) sum += (cleanNip[i] - '0') * weights[i];
        int controlSum = sum % 11;
        if (controlSum == 10 || controlSum != (cleanNip[9] - '0')) throw new ArgumentException($"Invalid NIP control sum: {cleanNip}.");
    }
}
