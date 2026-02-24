using System.Xml.Linq;
using CommandLine;

namespace KCKSeFCli;

[Verb("XMLRemoveNamespace", HelpText = "Removes namespaces from an XML invoice and sets a default namespace.")]
public class XMLRemoveNamespaceCommand : IGlobalCommand
{
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    [Value(1, Required = true, HelpText = "Output XML file path.")]
    public required string OutputFile { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        ConfigureLogging();

        if (!File.Exists(InputFile))
        {
            Log.Error($"Error: Input file not found: {InputFile}");
            return 1;
        }

        var xml = await File.ReadAllTextAsync(InputFile, cancellationToken).ConfigureAwait(false);
        var doc = XDocument.Parse(xml);

        var strippedDoc = MyXml.StripNamespacesFromDocument(doc);

        foreach (var element in strippedDoc.Descendants())
        {
            MyXml.SetDefaultXmlNamespace(element, MyXml.KsefNamespace);
        }
        
        // The default namespace needs to be declared on the root
        strippedDoc.Root?.SetAttributeValue("xmlns", MyXml.KsefNamespace.NamespaceName);

        await File.WriteAllTextAsync(OutputFile, strippedDoc.ToString() + "\n", cancellationToken).ConfigureAwait(false);

        return 0;
    }
}
