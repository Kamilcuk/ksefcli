using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using CommandLine;
namespace KCKSeFCli;

[Verb("XMLExtract", HelpText = "Extracts a value from an XML file using an XPath expression.")]
public class XMLExtractCommand : IGlobalCommand {
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }
    [Value(1, Required = true, HelpText = "XPath expression to extract the value.")]
    public required string XPathExpression { get; set; }
    [Option("namespaces", Required = false, HelpText = "Comma-separated list of namespace prefixes and URIs (e.g., 'ns=http://example.com,ns2=http://another.com').")]
    public string? Namespaces { get; set; }
    [Option('s', "strip-namespaces", Required = false, HelpText = "Strip all namespaces from the XML before querying, allowing plain XPath without prefixes.")]
    public bool StripNamespaces { get; set; }

    private static XDocument StripNamespacesFromDocument(XDocument doc) {
        return new XDocument(
            doc.Declaration,
            StripNamespacesFromNode(doc.Root!)
        );
    }

    private static XElement StripNamespacesFromNode(XElement element) {
        return new XElement(
            element.Name.LocalName,
            element.Attributes()
                .Where(a => !a.IsNamespaceDeclaration)
                .Select(a => new XAttribute(a.Name.LocalName, a.Value)),
            element.Nodes().Select(n => n is XElement child ? StripNamespacesFromNode(child) : n)
        );
    }

    public override Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        ConfigureLogging();
        if (!File.Exists(InputFile)) {
            Console.Error.WriteLine($"Error: Input file not found: {InputFile}");
            return Task.FromResult(1);
        }
        try {
            XDocument doc = XDocument.Load(InputFile);

            if (StripNamespaces) {
                doc = StripNamespacesFromDocument(doc);
            }

            XPathNavigator navigator = doc.CreateNavigator();
            XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);

            if (!StripNamespaces) {
                // Auto-register the default namespace from the root element
                XNamespace? defaultNamespace = doc.Root?.GetDefaultNamespace();
                if (defaultNamespace != null && !string.IsNullOrEmpty(defaultNamespace.NamespaceName)) {
                    manager.AddNamespace("default", defaultNamespace.NamespaceName);
                }

                // Auto-register any prefixed namespaces declared on the root element
                if (doc.Root != null) {
                    foreach (XAttribute? attr in doc.Root.Attributes().Where(a => a.IsNamespaceDeclaration)) {
                        string? prefix = attr.Name.LocalName == "xmlns" ? null : attr.Name.LocalName;
                        if (prefix != null && !manager.HasNamespace(prefix)) {
                            manager.AddNamespace(prefix, attr.Value);
                        }
                    }
                }

                // Register user-provided namespaces (these take precedence / can override)
                if (!string.IsNullOrEmpty(Namespaces)) {
                    foreach (string nsDecl in Namespaces.Split(',')) {
                        string[] parts = nsDecl.Split('=');
                        if (parts.Length == 2) {
                            manager.AddNamespace(parts[0].Trim(), parts[1].Trim());
                        }
                    }
                }
            }

            XPathNavigator? element = navigator.SelectSingleNode(XPathExpression, manager);
            if (element != null) {
                Console.WriteLine(element.Value);
            } else {
                Console.Error.WriteLine($"Error: Element not found for XPath: {XPathExpression}");
                return Task.FromResult(1);
            }
            return Task.FromResult(0);
        } catch (Exception ex) {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            return Task.FromResult(1);
        }
    }
}
