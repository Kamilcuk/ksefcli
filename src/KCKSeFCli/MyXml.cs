using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace KCKSeFCli;

public static class MyXml {
    public static XDocument StripNamespacesFromDocument(XDocument doc) {
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

    public static string XmlToString(XDocument doc, XNamespace ns) {
        foreach (XElement el in doc.Descendants()) {
            SetDefaultXmlNamespace(el, ns);
        }

        using MemoryStream ms = new MemoryStream();
        // Use UTF8 without BOM to avoid "Data at the root level is invalid" parsing errors in XmlReader
        XmlWriterSettings settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using (XmlWriter writer = XmlWriter.Create(ms, settings)) {
            doc.Save(writer);
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public static void SetDefaultXmlNamespace(XElement xelem, XNamespace xmlns) {
        if (xelem.Name.NamespaceName == string.Empty) {
            xelem.Name = xmlns + xelem.Name.LocalName;
        }

        foreach (XElement e in xelem.Elements()) {
            SetDefaultXmlNamespace(e, xmlns);
        }
    }

    public static XElement WithDefaultXmlNamespace(XElement xelem, XNamespace xmlns) {
        XName name;
        if (xelem.Name.NamespaceName == string.Empty) {
            name = xmlns + xelem.Name.LocalName;
        } else {
            name = xelem.Name;
        }

        return new XElement(name,
            (from e in xelem.Elements()
             select WithDefaultXmlNamespace(e, xmlns)));
    }

    public static void RegisterNamespaces(XDocument doc, XmlNamespaceManager manager, string? namespaces) {
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
        if (!string.IsNullOrEmpty(namespaces)) {
            foreach (string nsDecl in namespaces.Split(',')) {
                string[] parts = nsDecl.Split('=');
                if (parts.Length == 2) {
                    manager.AddNamespace(parts[0].Trim(), parts[1].Trim());
                }
            }
        }
    }
}
