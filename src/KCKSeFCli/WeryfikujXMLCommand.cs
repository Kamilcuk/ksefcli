using CommandLine;

namespace KCKSeFCli;

[Verb("WeryfikujXML", HelpText = "Validate KSeF XML invoice against the XSD schema.")]
public class WeryfikujXMLCommand : IGlobalCommand {
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        ConfigureLogging();

        if (!File.Exists(InputFile)) {
            Console.Error.WriteLine($"Error: Input file not found: {InputFile}");
            return 1;
        }

        string xmlContent = await File.ReadAllTextAsync(InputFile, cancellationToken).ConfigureAwait(false);
        if (XmlValidator.ValidateLog(xmlContent, out _)) {
            Log.Information("XML validation successful.");
            return 0;
        }
        return 1;
    }
}
