using System.Xml.Linq;

using CommandLine;

namespace KCKSeFCli;

[Verb("XMLRemoveNamespace", HelpText = "Removes namespaces from an XML invoice and sets a default namespace.")]
public class XMLRemoveNamespaceCommand : IGlobalCommand {
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    [Value(1, Required = true, HelpText = "Output XML file path.")]
    public required string OutputFile { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        ConfigureLogging();
        if (!File.Exists(InputFile)) {
            Log.Error($"Error: Input file not found: {InputFile}");
            return 1;
        }
        string xml = File.ReadAllText(InputFile);
        XDocument doc = XDocument.Parse(xml);
        doc = MyXml.NormalizeToNamespace(doc);
        string xmlString = MyXml.XmlToString(doc);
        File.WriteAllText(OutputFile, xmlString);
        return 0;
    }
}
