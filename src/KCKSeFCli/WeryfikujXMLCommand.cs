using System.Xml;
using System.Xml.Schema;
using CommandLine;

namespace KCKSeFCli;

[Verb("WeryfikujXML", HelpText = "Validate KSeF XML invoice against the XSD schema.")]
public class WeryfikujXMLCommand : IGlobalCommand
{
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        ConfigureLogging();

        if (!File.Exists(InputFile))
        {
            Console.Error.WriteLine($"Error: Input file not found: {InputFile}");
            return 1;
        }

        var xmlContent = await File.ReadAllTextAsync(InputFile, cancellationToken).ConfigureAwait(false);
        if (XmlValidator.Validate(xmlContent, out var errors))
        {
            Console.WriteLine("XML validation completed successfully.");
            return 0;
        }
        else
        {
            Console.Error.WriteLine("XML validation failed:");
            foreach (var error in errors)
            {
                Console.Error.WriteLine(error);
            }
            return 1;
        }
    }
}
