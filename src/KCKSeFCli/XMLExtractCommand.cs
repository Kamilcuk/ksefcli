using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using CommandLine;

namespace KCKSeFCli;

[Verb("XMLExtract", HelpText = "Extracts a value from an XML file using an XPath expression.")]
public class XMLExtractCommand : IGlobalCommand
{
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    [Value(1, Required = true, HelpText = "XPath expression to extract the value.")]
    public required string XPathExpression { get; set; }

    [Option("namespaces", Required = false, HelpText = "Comma-separated list of namespace prefixes and URIs (e.g., 'ns=http://example.com,ns2=http://another.com').")]
    public string? Namespaces { get; set; }

    public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        ConfigureLogging();

        if (!File.Exists(InputFile))
        {
            Console.Error.WriteLine($"Error: Input file not found: {InputFile}");
            return Task.FromResult(1);
        }

        try
        {
            var doc = XDocument.Load(InputFile);
            XPathNavigator navigator = doc.CreateNavigator();
            XmlNamespaceManager? manager = null;

            if (!string.IsNullOrEmpty(Namespaces))
            {
                manager = new XmlNamespaceManager(navigator.NameTable);
                foreach (var nsDecl in Namespaces.Split(','))
                {
                    var parts = nsDecl.Split('=');
                    if (parts.Length == 2)
                    {
                        manager.AddNamespace(parts[0], parts[1]);
                    }
                }
            }
            
            var element = navigator.SelectSingleNode(XPathExpression, manager);

            if (element != null)
            {
                Console.WriteLine(element.Value);
            }
            else
            {
                Console.Error.WriteLine($"Error: Element not found for XPath: {XPathExpression}");
                return Task.FromResult(1);
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            return Task.FromResult(1);
        }
    }
}
