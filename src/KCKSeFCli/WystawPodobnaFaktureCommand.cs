using System.Xml.Linq;
using CommandLine;
using System.Text;
using System.Xml;

namespace KCKSeFCli;

[Verb("WystawPodobnaFakture", HelpText = "Create a new KSeF XML invoice based on an existing one with updated dates.")]
public class WystawPodobnaFaktureCommand : IGlobalCommand
{
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    [Value(1, Required = true, HelpText = "Output XML file path.")]
    public required string OutputFile { get; set; }

    [Option("data-wystawienia", Required = false, HelpText = "New date of issuance (P_1). Format: yyyy-MM-dd. Defaults to today.")]
    public string? DataWystawienia { get; set; }

    [Option("data-wykonania", Required = false, HelpText = "New date of supply (P_6). Format: yyyy-MM-dd. Defaults to today.")]
    public string? DataWykonania { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        ConfigureLogging();

        if (!File.Exists(InputFile))
        {
            Console.Error.WriteLine($"Error: Input file not found: {InputFile}");
            return 1;
        }

        var xmlContent = await File.ReadAllTextAsync(InputFile, cancellationToken).ConfigureAwait(false);
        var doc = XDocument.Parse(xmlContent);
        XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string p1Date = DataWystawienia ?? today;
        string p6Date = DataWykonania ?? today;

        var naglowek = doc.Root?.Element(ns + "Naglowek");
        if (naglowek != null)
        {
            var dataWytworzenia = naglowek.Element(ns + "DataWytworzeniaFa");
            if (dataWytworzenia != null)
            {
                dataWytworzenia.Value = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
        }

        var fa = doc.Root?.Element(ns + "Fa");
        if (fa != null)
        {
            var p1 = fa.Element(ns + "P_1");
            if (p1 != null) p1.Value = p1Date;

            var p6 = fa.Element(ns + "P_6");
            if (p6 != null) p6.Value = p6Date;

            // Update P_2 (Invoice number) based on new P_1 if it follows the pattern FV/yyyyMMdd/01
            var p2 = fa.Element(ns + "P_2");
            if (p2 != null && p2.Value.StartsWith("FV/"))
            {
                 string datePart = p1Date.Replace("-", "");
                 p2.Value = $"FV/{datePart}/01";
            }
        }

        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
        {
            doc.Save(writer);
        }
        string newXml = Encoding.UTF8.GetString(ms.ToArray());
        await File.WriteAllTextAsync(OutputFile, newXml, cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"Successfully created similar invoice: {OutputFile}");
        return 0;
    }
}
