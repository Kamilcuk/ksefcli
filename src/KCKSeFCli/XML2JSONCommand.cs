using System.Text.Json;
using System.Xml.Linq;

using CommandLine;

namespace KCKSeFCli;

[Verb("XML2JSON", HelpText = "Convert invoice XML file(s) to JSON. Usage: xml2json input.xml [output.json]  or  xml2json input1.xml input2.xml outputdir/")]
public class XML2JSONCommand : ConversionCommandBase {
    [Option("indent", HelpText = "Indent JSON output.")]
    public bool Indent { get; set; } = true;

    public override Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        ConfigureLogging();

        var (inputFiles, outputFile, outputDir) = ParseArgs();
        if (inputFiles == null) return Task.FromResult(1);

        var jsonOptions = new JsonSerializerOptions {
            WriteIndented = Indent
        };

        foreach (var inputFile in inputFiles) {
            if (!ValidateInputFile(inputFile)) return Task.FromResult(1);

            XDocument doc = XDocument.Load(inputFile);
            object jsonObj = XElementToObject(doc.Root!);
            string json = JsonSerializer.Serialize(jsonObj, jsonOptions);

            string? outputJsonPath = GetOutputPath(inputFile, outputFile, outputDir, ".json", allowStdout: true);
            if (outputJsonPath == null) return Task.FromResult(1);

            if (outputJsonPath == "-") {
                Console.WriteLine(json);
                continue;
            }

            if (CheckOutputExists(outputJsonPath)) return Task.FromResult(1);

            File.WriteAllText(outputJsonPath, json);
            Log.Information($"Saved JSON to {outputJsonPath}");
        }

        return Task.FromResult(0);
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