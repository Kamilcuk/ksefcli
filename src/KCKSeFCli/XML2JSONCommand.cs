using System.Text.Json;
using System.Xml.Linq;

using CommandLine;

namespace KCKSeFCli;

[Verb("XML2JSON", HelpText = "Convert invoice XML to JSON.")]
public class XML2JSONCommand : IGlobalCommand {
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    [Option('o', "output", HelpText = "Output JSON file path. If not specified, writes to stdout.")]
    public string? OutputFile { get; set; }

    [Option("indent", HelpText = "Indent JSON output.")]
    public bool Indent { get; set; } = true;

    public override Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        ConfigureLogging();
        if (!File.Exists(InputFile)) {
            Console.Error.WriteLine($"Error: Input file not found: {InputFile}");
            return Task.FromResult(1);
        }

        XDocument doc = XDocument.Load(InputFile);
        var jsonOptions = new JsonSerializerOptions {
            WriteIndented = Indent
        };

        object jsonObj = XElementToObject(doc.Root!);
        string json = JsonSerializer.Serialize(jsonObj, jsonOptions);

        if (OutputFile != null) {
            File.WriteAllText(OutputFile, json);
            Log.Information($"Saved JSON to {OutputFile}");
        } else {
            Console.WriteLine(json);
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