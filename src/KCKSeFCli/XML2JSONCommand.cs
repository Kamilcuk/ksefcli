using System.Text;
using System.Text.Json;
using System.Xml.Linq;

using CommandLine;

namespace KCKSeFCli;

[Verb("XML2JSON", HelpText = "Convert invoice XML file(s) to JSON. Usage: xml2json input.xml [output.json]  or  xml2json input1.xml input2.xml outputdir/")]
public class XML2JSONCommand : ConversionCommandBase {
    [Option("indent", HelpText = "Indent JSON output.")]
    public bool Indent { get; set; } = true;

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        ConfigureLogging();

        var parsedArgs = ParseArgs();
        if (parsedArgs == null) return 1;

        // If no output file or directory is specified and we have exactly one input file, then use stdout
        if (parsedArgs.OutputFile == null && parsedArgs.OutputDir == null && parsedArgs.InputFiles.Count() == 1) {
            parsedArgs = parsedArgs with { OutputFile = "-" };
        }

        return await ProcessListOfFiles(
            parsedArgs,
            ".json",
            this,
            async (inputFile, self, ct) => {
                XDocument doc = XDocument.Load(inputFile);
                object jsonObj = XElementToObject(doc.Root!);
                var jsonOptions = new JsonSerializerOptions {
                    WriteIndented = self.Indent
                };
                string json = JsonSerializer.Serialize(jsonObj, jsonOptions);
                return Encoding.UTF8.GetBytes(json);
            },
            cancellationToken,
            allowStdout: true
        ).ConfigureAwait(false);
    }

    internal static object XElementToObject(XElement element) {
        var dict = new Dictionary<string, object>();

        foreach (XAttribute attr in element.Attributes()) {
            dict["@" + attr.Name.LocalName] = attr.Value;
        }

        var childElements = element.Elements().ToList();
        if (childElements.Count == 0) {
            string value = element.Value;
            if (dict.Count == 0) {
                return value;
            }
            dict["#text"] = value;
            return dict;
        }

        var grouped = childElements
            .GroupBy(e => e.Name.LocalName)
            .ToDictionary(g => g.Key, g => g.Count() == 1 ? (object)XElementToObject(g.First()) : g.Select(XElementToObject).ToList());

        foreach (var kvp in grouped) {
            dict[kvp.Key] = kvp.Value;
        }

        return dict;
    }
}