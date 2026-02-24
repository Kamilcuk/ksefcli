using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace KCKSeFCli;

public static class MyXml {
    public static readonly XNamespace KsefNamespace = "http://crd.gov.pl/wzor/2025/06/25/13775/";
    public static readonly XNamespace XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";


    public static string XmlToString(XDocument doc) {
        using (MemoryStream ms = new MemoryStream()) {
            XmlWriterSettings settings = new XmlWriterSettings {
                Indent = true,
                Encoding = new UTF8Encoding(false)
            };
            using (XmlWriter writer = XmlWriter.Create(ms, settings)) {
                doc.Save(writer);
            }
            return Encoding.UTF8.GetString(ms.ToArray()) + "\n";
        }
    }

    public static XDocument Normalize(XDocument doc) {
        if (doc.Root == null) return new XDocument(doc.Declaration);

        XNamespace ns = MyXml.KsefNamespace;
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        XNamespace etd = "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/05/eD/DefinicjeTypy/";

        XElement Map(XElement el) => new XElement(
            ns + el.Name.LocalName,
            el.Attributes()
                .Where(a => !a.IsNamespaceDeclaration)
                .Select(a => new XAttribute(a.Name.LocalName, a.Value)),
            el.Nodes().Select(n => n is XElement c ? Map(c) : n)
        );

        XElement root = Map(doc.Root);

        // Manual injection of required declarations and schemaLocation
        root.Add(
            new XAttribute(XNamespace.Xmlns + "etd", etd.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName)
        // new XAttribute(xsi + "schemaLocation", ns.NamespaceName) 
        );

        return new XDocument(doc.Declaration, root);
    }

    public static void SetDefaultXmlNamespace(XElement xelem, XNamespace xmlns) {
        if (xelem.Name.NamespaceName == string.Empty) {
            xelem.Name = xmlns + xelem.Name.LocalName;
        }
        foreach (XElement e in xelem.Elements()) {
            SetDefaultXmlNamespace(e, xmlns);
        }
    }

    public static XDocument StripNamespacesFromDocument(XDocument doc) {
        if (doc.Root == null) return new XDocument(doc.Declaration);
        XElement Strip(XElement el) => new XElement(
            el.Name.LocalName,
            el.Attributes()
                .Where(a => !a.IsNamespaceDeclaration)
                .Select(a => new XAttribute(a.Name.LocalName, a.Value)),
            el.Nodes().Select(n => n is XElement c ? Strip(c) : n)
        );
        return new XDocument(doc.Declaration, Strip(doc.Root));
    }

    public static XDocument NormalizeToNamespace(XDocument doc) {
        if (doc.Root == null) return new XDocument(doc.Declaration);
        XNamespace targetNs = doc.Root.Name.Namespace;
        if (targetNs == XNamespace.None) targetNs = MyXml.KsefNamespace;
        XElement Map(XElement el) => new XElement(
            targetNs + el.Name.LocalName,
            el.Attributes()
            .Where(a => !a.IsNamespaceDeclaration ||
                        a.Name.LocalName == "xsi" ||
                        a.Name.LocalName == "etd")
                .Select(a => {
                    if (a.Name.Namespace == XNamespace.Xmlns) return a;
                    return new XAttribute(a.Name.LocalName, a.Value);
                }),
            el.Nodes().Select(n => n is XElement c ? Map(c) : n)
        );
        return new XDocument(doc.Declaration, Map(doc.Root));
    }

    public static bool HasExactlyOneNamespace(XDocument doc, out XNamespace detectedNs) {
        List<XNamespace> namespaces = doc.Descendants()
            .Select(x => x.Name.Namespace)
            .Where(ns => ns != XNamespace.None &&
                         ns != XNamespace.Xml &&
                         ns != "http://www.w3.org/2001/XMLSchema-instance")
            .Distinct()
            .ToList();
        detectedNs = namespaces.FirstOrDefault() ?? XNamespace.None;
        return namespaces.Count == 1;
    }

    public static void RegisterNamespaces(XDocument doc, XmlNamespaceManager manager) {
        if (doc.Root == null) return;
        // Register default
        XNamespace dns = doc.Root.GetDefaultNamespace();
        if (!string.IsNullOrEmpty(dns.NamespaceName))
            manager.AddNamespace("default", dns.NamespaceName);
        // Register existing prefixes
        foreach (XAttribute? attr in doc.Root.Attributes().Where(a => a.IsNamespaceDeclaration)) {
            string? prefix = attr.Name.LocalName == "xmlns" ? null : attr.Name.LocalName;
            if (prefix != null) manager.AddNamespace(prefix, attr.Value);
        }
    }
}
