using System.Globalization;
using System.Xml.Linq;

using CommandLine;

namespace KCKSeFCli;

[Verb("DodajPozycjeNaFakturze", HelpText = "Add a new item to an existing KSeF XML invoice.")]
public class DodajPozycjeNaFakturzeCommand : IGlobalCommand {
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    [Value(1, Required = false, HelpText = "Output XML file path. If not provided, the input file will be overwritten.")]
    public string? OutputFile { get; set; }

    [Option("nazwa", Required = true, HelpText = "Name of the good or service (P_7).")]
    public required string Nazwa { get; set; }

    [Option("miara", Required = true, HelpText = "Unit of measure (P_8A).")]
    public required string Miara { get; set; }

    [Option("ilosc", Required = true, HelpText = "Quantity (P_8B).")]
    public required decimal Ilosc { get; set; }

    [Option("cena-netto", Required = true, HelpText = "Unit net price (P_9A).")]
    public required decimal CenaNetto { get; set; }

    [Option("stawka-vat", Required = true, HelpText = "VAT rate (P_12), e.g., 23, 8, 5, 0.")]
    public required string StawkaVat { get; set; }

    [Option("bez-walidacji", Required = false, HelpText = "Skip XML validation after adding the item.")]
    public bool BezWalidacji { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        ConfigureLogging();

        if (!File.Exists(InputFile)) {
            Log.Error($"Error: Input file not found: {InputFile}");
            return 1;
        }

        string outputPath = OutputFile ?? InputFile;

        string xml = File.ReadAllText(InputFile);

        XDocument doc = XDocument.Parse(xml);
        XNamespace ns = MyXml.KsefNamespace;

        XElement? fa = doc.Root?.Element(ns + "Fa");
        if (fa == null) {
            Log.Error("Error: Could not find <Fa> element in the XML.");
            return 1;
        }

        XElement? lastWiersz = fa.Elements(ns + "FaWiersz").LastOrDefault();
        if (lastWiersz == null) {
            Log.Error("Error: Could not find any <FaWiersz> elements in the XML.");
            return 1;
        }

        int newWierszId = int.Parse(lastWiersz.Element(ns + "NrWierszaFa")?.Value ?? "0") + 1;
        decimal wartoscNetto = Ilosc * CenaNetto;

        XElement newFaWiersz = new XElement(ns + "FaWiersz",
            new XElement(ns + "NrWierszaFa", newWierszId.ToString()),
            new XElement(ns + "P_7", Nazwa),
            new XElement(ns + "P_8A", Miara),
            new XElement(ns + "P_8B", Ilosc.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement(ns + "P_9A", CenaNetto.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement(ns + "P_11", wartoscNetto.ToString("F2", CultureInfo.InvariantCulture)),
            new XElement(ns + "P_12", StawkaVat)
        );

        lastWiersz.AddAfterSelf(newFaWiersz);

        if (StawkaVat == "23" || StawkaVat == "22") {
            XElement? p13_1 = fa.Element(ns + "P_13_1");
            if (p13_1 != null) {
                decimal currentValue = decimal.Parse(p13_1.Value, CultureInfo.InvariantCulture);
                p13_1.Value = (currentValue + wartoscNetto).ToString("F2", CultureInfo.InvariantCulture);
            }

            XElement? p14_1 = fa.Element(ns + "P_14_1");
            if (p14_1 != null) {
                decimal currentVat = decimal.Parse(p14_1.Value, CultureInfo.InvariantCulture);
                decimal newVat = wartoscNetto * (decimal.Parse(StawkaVat, CultureInfo.InvariantCulture) / 100);
                p14_1.Value = (currentVat + newVat).ToString("F2", CultureInfo.InvariantCulture);
            }
        }

        XElement? p15 = fa.Element(ns + "P_15");
        if (p15 != null) {
            decimal currentTotal = decimal.Parse(p15.Value, CultureInfo.InvariantCulture);
            decimal newVat = (StawkaVat == "23" || StawkaVat == "22") ? wartoscNetto * (decimal.Parse(StawkaVat, CultureInfo.InvariantCulture) / 100) : 0;
            p15.Value = (currentTotal + wartoscNetto + newVat).ToString("F2", CultureInfo.InvariantCulture);
        }

        doc = MyXml.Normalize(doc);
        string newXml = MyXml.XmlToString(doc);

        File.WriteAllText(outputPath, newXml);
        Log.Information($"Successfully added item and saved to: {outputPath}");

        if (!BezWalidacji) {
            if (XmlValidator.Validate(newXml, out List<string>? errors)) {
                Log.Information("Post-modification validation successful.");
            } else {
                Log.Error("Post-modification validation failed:");
                foreach (string error in errors!) {
                    Log.Error(error);
                }
                return 1;
            }
        }

        return 0;
    }
}
