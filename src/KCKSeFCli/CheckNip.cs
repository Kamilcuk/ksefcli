using System.Text.RegularExpressions;

namespace KCKSeFCli;

public static partial class CheckNip
{
    [GeneratedRegex(@".*\|nip-(\d+)\|.*", RegexOptions.Compiled)]
    private static partial Regex NipRegex();

    public static string ExtractNipFromToken(string token)
    {
        Match match = NipRegex().Match(token);
        if (!match.Success)
        {
            throw new InvalidOperationException("You have to specify a token that contains nip.");
        }
        string nip = match.Groups[1].Value;
        if (string.IsNullOrEmpty(nip))
        {
            throw new InvalidOperationException("You have to specify a token that contains nip.");
        }
        if (!CheckNip.IsNipValid(nip))
        {
            throw new InvalidOperationException($"Invalid NIP format: {nip}");
        }
        return nip;

    }

    public static bool IsNipValid(string? nip)
    {
        if (string.IsNullOrEmpty(nip))
        {
            return false;
        }
        nip = nip.Replace("-", string.Empty).Trim();
        if (nip.Length != 10)
        {
            return false;
        }
        if (!long.TryParse(nip, out _))
        {
            return false;
        }
        int[] weights = { 6, 5, 7, 2, 1, 4, 3, 10, 5, 0 };
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += (nip[i] - '0') * weights[i];
        }
        int controlSum = sum % 11;
        if (controlSum == 10)
        {
            return false;
        }
        return controlSum == (nip[9] - '0');
    }
}
