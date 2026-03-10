using System.Globalization;

using CommandLine;

namespace KCKSeFCli;

[Verb("ParseDate", HelpText = "Parse a date string and output it in ISO 8601 format or seconds since epoch.")]
public class ParseDateCommand : IGlobalCommand {
    [Value(0, Required = true, HelpText = "The date string to parse.")]
    public required string DateString { get; set; }

    [Option("seconds", HelpText = "Output floating point number of seconds since linux epoch.")]
    public bool Seconds { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        DateTime result = await ParseDate.Parse(DateString, cancellationToken).ConfigureAwait(false);
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
