using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using HumanDateParser;

namespace KCKSeFCli;

public static class ParseDate {
    private static readonly string[] DayUnits = { "day", "days", "dzien", "dzień", "dni" };
    private static readonly string[] WeekUnits = { "week", "weeks", "tydzień", "tygodni" };
    private static readonly string[] MonthUnits = { "month", "months", "miesiąc", "miesiac", "miesiące", "miesiace" };
    private static readonly string[] YearUnits = { "year", "years", "rok", "roków", "lata" };

    private static readonly string DayUnitsPattern = string.Join("|", DayUnits);
    private static readonly string WeekUnitsPattern = string.Join("|", WeekUnits);
    private static readonly string MonthUnitsPattern = string.Join("|", MonthUnits);
    private static readonly string YearUnitsPattern = string.Join("|", YearUnits);
    private static readonly string AllUnitsPattern = string.Join("|", DayUnits.Concat(WeekUnits).Concat(MonthUnits).Concat(YearUnits));

    private static readonly Dictionary<string, Func<DateTime, int, DateTime>> UnitHandlers = new(StringComparer.OrdinalIgnoreCase) {
        // Days
        ["day"] = (d, n) => d.AddDays(-n),
        ["days"] = (d, n) => d.AddDays(-n),
        ["dzien"] = (d, n) => d.AddDays(-n),
        ["dzień"] = (d, n) => d.AddDays(-n),
        ["dni"] = (d, n) => d.AddDays(-n),
        // Weeks
        ["week"] = (d, n) => d.AddDays(-n * 7),
        ["weeks"] = (d, n) => d.AddDays(-n * 7),
        ["tydzień"] = (d, n) => d.AddDays(-n * 7),
        ["tygodni"] = (d, n) => d.AddDays(-n * 7),
        // Months
        ["month"] = (d, n) => d.AddMonths(-n),
        ["months"] = (d, n) => d.AddMonths(-n),
        ["miesiąc"] = (d, n) => d.AddMonths(-n),
        ["miesiac"] = (d, n) => d.AddMonths(-n),
        ["miesiące"] = (d, n) => d.AddMonths(-n),
        ["miesiace"] = (d, n) => d.AddMonths(-n),
        // Years
        ["year"] = (d, n) => d.AddYears(-n),
        ["years"] = (d, n) => d.AddYears(-n),
        ["rok"] = (d, n) => d.AddYears(-n),
        ["roków"] = (d, n) => d.AddYears(-n),
        ["lata"] = (d, n) => d.AddYears(-n),
    };

    private static DateTime? ParseRelativeDate(string dateString, DateTime currentDate) {
        // Handle "now" and "teraz" (Polish for "now")
        if (string.Equals(dateString, "now", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dateString, "teraz", StringComparison.OrdinalIgnoreCase)) {
            Log.Debug($"Parsed '{dateString}' as now: {currentDate}");
            return currentDate;
        }

        // Handle chained units: "2 days 3 weeks", "2 dni i 2 lata", "2 years 2 miesiace", etc.
        // Pattern: number unit [separator] number unit ... [optional "temu" at end]
        var chainedPattern = $@"^(?<parts>(?:\s*-?\d+\s*(?:{AllUnitsPattern})\s*(?:i\s+|,?\s+)?)+)\s*(?:temu)?$";
        var match = Regex.Match(dateString, chainedPattern, RegexOptions.IgnoreCase);
        if (match.Success) {
            string partsStr = match.Groups["parts"].Value;
            // Extract all number-unit pairs
            var partPattern = $@"(?<sign>-?)\s*(?<number>\d+)\s*(?<unit>{AllUnitsPattern})";
            var partMatches = Regex.Matches(partsStr, partPattern, RegexOptions.IgnoreCase);

            DateTime calculatedDate = currentDate;
            bool anyMatched = false;

            foreach (Match partMatch in partMatches) {
                int number = int.Parse(partMatch.Groups["number"].Value);
                string unit = partMatch.Groups["unit"].Value;
                // Sign is ignored - all relative dates mean "ago" (past)
                if (UnitHandlers.TryGetValue(unit, out var handler)) {
                    calculatedDate = handler(calculatedDate, number);
                    anyMatched = true;
                    Log.Debug($"Parsed part '{partMatch.Value}' -> {calculatedDate}");
                }
            }

            if (anyMatched) {
                Log.Debug($"Parsed '{dateString}' using chained units: {calculatedDate}");
                return calculatedDate;
            }
        }

        // Handle simple relative dates like -2days, 3weeks, 1month, 2months, 1year, 2years
        // Both positive and negative mean "ago" (past)
        string simplePattern = $@"^-?(?<number>\d+)(?<unit>{AllUnitsPattern})$";
        match = Regex.Match(dateString, simplePattern, RegexOptions.IgnoreCase);
        if (match.Success) {
            int number = int.Parse(match.Groups["number"].Value);
            string unit = match.Groups["unit"].Value;

            if (UnitHandlers.TryGetValue(unit, out var handler)) {
                DateTime calculatedDate = handler(currentDate, number);
                Log.Debug($"Parsed '{dateString}' using simple pattern: {calculatedDate}");
                return calculatedDate;
            }
        }

        // Handle Polish natural language with "temu": "2 lata temu", "2 miesiace temu", "2 dni temu"
        var temuPattern = $@"^(?<number>\d+)\s*(?<unit>{AllUnitsPattern})\s+(?:temu| temu)$";
        match = Regex.Match(dateString, temuPattern, RegexOptions.IgnoreCase);
        if (match.Success) {
            int number = int.Parse(match.Groups["number"].Value);
            string unit = match.Groups["unit"].Value;

            if (UnitHandlers.TryGetValue(unit, out var handler)) {
                DateTime calculatedDate = handler(currentDate, number);
                Log.Debug($"Parsed '{dateString}' using 'temu' pattern: {calculatedDate}");
                return calculatedDate;
            }
        }

        return null;
    }

    private static void ValidateDateRange(DateTime date, string originalInput) {
        if (date.Year < 1900 || date.Year > 2100) {
            throw new FormatException($"Parsed date {date:yyyy-MM-dd} from '{originalInput}' is outside valid range (1900-2100)");
        }
    }

    public static async Task<DateTime> Parse(string dateString, CancellationToken cancellationToken, DateTime? currentDate = null) {
        DateTime now = currentDate ?? DateTime.Now;

        // 1. Try parsing using standard C# DateTime.Parse
        if (DateTime.TryParse(dateString, out DateTime result)) {
            ValidateDateRange(result, dateString);
            Log.Debug($"Parsed '{dateString}' using DateTime.TryParse: {result}");
            return result;
        }

        // Try parsing with specific formats if standard parsing fails
        string[] formats = {
            "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "dd-MM-yyyy", "dd-MM-yyyy HH:mm:ss",
            "yyyy/MM/dd", "yyyy/MM/dd HH:mm:ss"
        };
        if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) {
            ValidateDateRange(result, dateString);
            Log.Debug($"Parsed '{dateString}' using DateTime.TryParseExact: {result}");
            return result;
        }

        // 2. Parse relative dates
        DateTime? relativeDate = ParseRelativeDate(dateString, now);
        if (relativeDate.HasValue) {
            ValidateDateRange(relativeDate.Value, dateString);
            return relativeDate.Value;
        }

        // 3. Use HumanDateParser
        try {
            DateTime parsed = DateParser.Parse(dateString);
            ValidateDateRange(parsed, dateString);
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
                ValidateDateRange(result, dateString);
                Log.Debug($"Parsed '{dateString}' using GNU date: {result}");
                return result;
            }
        } catch (Exception ex) {
            Log.Debug($"GNU date fallback failed for '{dateString}': {ex.Message}");
        }

        throw new FormatException($"Could not parse date string: '{dateString}'\n" +
                "Supported formats:\n" +
                "  - ISO 8601: yyyy-MM-dd, yyyy-MM-ddTHH:mm:ss, dd-MM-yyyy, etc.\n" +
                "  - Relative (past): -Ndays, -Nweeks, -Nmonths, -Nyears (N = number)\n" +
                "  - Relative (past, positive also works): Ndays, Nweeks, Nmonths, Nyears\n" +
                "  - Polish units: dzien/dni, tydzien/tygodni, miesiac/miesiace, rok/roków/lata\n" +
                "  - Polish natural language: 'N lata temu', 'N miesiace temu', 'N dni temu'\n" +
                "  - Chained: '2 days 3 weeks', '2 dni i 2 lata', '2 years 2 miesiace', '2 lata i 2 miesiace temu'\n" +
                "  - Keywords: 'now', 'teraz'\n" +
                "  - Human-readable: 'last monday', 'yesterday', '2 weeks ago', etc. (via HumanDateParser)\n" +
                "  - GNU date formats (fallback): any format supported by 'date -d'");
    }
}