using System.Text.RegularExpressions;

namespace KCKSeFCli;

public static partial class CheckNip {
    private static readonly Regex _nipRegex = new Regex(@".*\|nip-(\d+)\|.*", RegexOptions.Compiled);
    private static Regex NipRegex() => _nipRegex;

    public static string ExtractNipFromToken(string token) {
        Match match = NipRegex().Match(token);
        if (!match.Success) {
            throw new InvalidOperationException("You have to specify a token that contains nip.");
        }
        string nip = match.Groups[1].Value;
        if (string.IsNullOrEmpty(nip)) {
            throw new InvalidOperationException("You have to specify a token that contains nip.");
        }
        CheckNip.AssertNipIsValid(nip);
        return nip;

    }

    public static void AssertNipIsValid(string? nip) {
        if (string.IsNullOrEmpty(nip)) {
            throw new ArgumentException("NIP cannot be null or empty.");
        }
        string cleanNip = nip!.Replace("-", string.Empty).Trim();
        if (cleanNip.Length != 10) {
            throw new ArgumentException($"Invalid NIP length: {cleanNip}. NIP must be 10 digits long.");
        }
        if (!long.TryParse(cleanNip, out _)) {
            throw new ArgumentException($"Invalid NIP format: {cleanNip}. NIP must be a number.");
        }
        int[] weights = { 6, 5, 7, 2, 3, 4, 5, 6, 7 };
        int sum = 0;
        for (int i = 0; i < 9; i++) {
            sum += (cleanNip[i] - '0') * weights[i];
        }
        int controlSum = sum % 11;
        if (controlSum == 10) {
            throw new ArgumentException($"Invalid NIP control sum: {cleanNip}.");
        }
        if (controlSum != (cleanNip[9] - '0')) {
            throw new ArgumentException($"Invalid NIP control digit: {cleanNip}.");
        }
    }
}
