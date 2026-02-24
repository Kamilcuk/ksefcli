using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using HumanDateParser;

namespace KCKSeFCli;

public static class ParseDate {
    private static DateTime? ParseRelativeDate(string dateString) {
        Regex regex = new Regex(@"^-(?<number>\d+)(?<unit>day|days|dzien|dzień|dni|week|weeks|tydzień|tygodni)$", RegexOptions.IgnoreCase);
        Match match = regex.Match(dateString);
        if (match.Success) {
            int number = int.Parse(match.Groups["number"].Value);
            string unit = match.Groups["unit"].Value.ToLower();

            DateTime baseDate = DateTime.Now;
            DateTime calculatedDate;

            if (unit == "day" || unit == "days" || unit == "dzien" || unit == "dzień" || unit == "dni") {
                calculatedDate = baseDate.AddDays(-number);
                Log.Debug($"Parsed '{dateString}' using Regex (days): {calculatedDate}");
                return calculatedDate;
            } else if (unit == "week" || unit == "weeks" || unit == "tydzień" || unit == "tygodni") {
                calculatedDate = baseDate.AddDays(-number * 7);
                Log.Debug($"Parsed '{dateString}' using Regex (weeks): {calculatedDate}");
                return calculatedDate;
            }
        }
        return null;
    }

    public static async Task<DateTime> Parse(string dateString, CancellationToken cancellationToken) {
        // 1. Try parsing using standard C# DateTime.Parse
        if (DateTime.TryParse(dateString, out DateTime result)) {
            Log.Debug($"Parsed '{dateString}' using DateTime.TryParse: {result}");
            return result;
        }

        // Try parsing with specific formats if standard parsing fails
        string[] formats = {
            "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy", "dd-MM-yyyy HH:mm:ss",
            "yyyy/MM/dd", "yyyy/MM/dd HH:mm:ss"
        };
        if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) {
            Log.Debug($"Parsed '{dateString}' using DateTime.TryParseExact: {result}");
            return result;
        }

        // 2. Parse relative dates
        DateTime? relativeDate = ParseRelativeDate(dateString);
        if (relativeDate.HasValue) {
            return relativeDate.Value;
        }

        // 3. Use HumanDateParser
        try {
            DateTime parsed = DateParser.Parse(dateString);
            Log.Debug($"Parsed '{dateString}' using HumanDateParser: {parsed}");
            return parsed;
        } catch {
            // HumanDateParser failed, proceed to fallback
        }

        // 4. Fallback to running GNU date
        try {
            string[] cmd = new[] { "date", "-d", dateString, "+%s.%N" };
            Subprocess subprocess = new Subprocess(CommandAndArgs: cmd, Quiet: true);
            byte[] outputBytes = await subprocess.CheckOutputAsync(cancellationToken).ConfigureAwait(false);
            string output = Encoding.UTF8.GetString(outputBytes).Trim();

            if (double.TryParse(output, NumberStyles.Float, CultureInfo.InvariantCulture, out double unixTimestampSeconds)) {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds((long)unixTimestampSeconds);
                dateTimeOffset = dateTimeOffset.AddSeconds(unixTimestampSeconds - (long)unixTimestampSeconds);
                result = dateTimeOffset.ToLocalTime().DateTime; // Explicitly convert to local time
                Log.Debug($"Parsed '{dateString}' using GNU date: {result}");
                return result;
            }
        } catch (Exception ex) {
            Log.Debug($"GNU date fallback failed for '{dateString}': {ex.Message}");
        }

        throw new FormatException($"Could not parse date string: {dateString}");
    }
}
