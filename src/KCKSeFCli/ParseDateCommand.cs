using System.Globalization;

using CommandLine;

namespace KCKSeFCli;

[Verb("ParseDate", HelpText = "Parse a date string and output it in ISO 8601 format or seconds since epoch.")]
public class ParseDateCommand : IGlobalCommand {
    [Value(0, Required = true, HelpText = "The date string to parse.")]
    public required string DateString { get; set; }

    [Option("seconds", HelpText = "Output floating point number of seconds since linux epoch.")]
    public bool Seconds { get; set; }

    [Option("now", HelpText = "Specify the current date/time to calculate relative dates from (ISO 8601 format). Defaults to current system time.")]
    public string? Now { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        DateTime? now = null;
        if (!string.IsNullOrEmpty(Now)) {
            if (!DateTime.TryParse(Now, out DateTime parsedNow)) {
                Console.Error.WriteLine($"Error: Could not parse --now value: {Now}");
                return 1;
            }
            now = parsedNow;
        }
        DateTime result = await ParseDate.Parse(DateString, cancellationToken, now).ConfigureAwait(false);
        if (Seconds) {
            TimeSpan diff = result.ToUniversalTime() - Compatibility.UnixEpoch;
            double seconds = diff.TotalSeconds;
            Console.WriteLine(seconds.ToString("F6", CultureInfo.InvariantCulture));
        } else {
            Console.WriteLine(result.ToString("yyyy-MM-ddTHH:mm:ss.ffffff"));
        }
        return 0;
    }
}
