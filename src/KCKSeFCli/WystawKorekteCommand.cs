using System.Globalization;
using System.Xml.Linq;

using CommandLine;

using KCKSeFCli;

namespace KCKSeFCli;

[Verb("WystawKorekte", HelpText = "Issue a correction invoice based on an input XML.")]
public class WystawKorekteCommand : IGlobalCommand {
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    [Value(1, Required = true, HelpText = "Output XML file path.")]
    public required string OutputFile { get; set; }

    [Value(2, Required = true, HelpText = "Pairs of arguments: <numer_lub_nazwa_pozycji> <nowa_ilosc_lub_roznica>")]
    public required IEnumerable<string> Korekty { get; set; }

    [Option("PrzyczynaKorekty", Default = "", HelpText = "Reason for correction (PrzyczynaKorekty).")]
    public required string PrzyczynaKorekty { get; set; }

    [Option("TypKorekty", HelpText = "Type of correction (TypKorekty).")]
    public string? TypKorekty { get; set; }

    [Option("no-validate", HelpText = "Skip XML validation after creating the correction.")]
    public bool NoValidate { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        ConfigureLogging();

        if (!File.Exists(InputFile)) {
            Log.Error($"Error: Input file not found: {InputFile}");
            return 1;
        }

        string xml = File.ReadAllText(InputFile);
        XDocument doc = XDocument.Parse(xml);
        XNamespace ns = MyXml.KsefNamespace;

        XElement? fa = doc.Root?.Element(ns + "Fa");
        if (fa == null) {
            Log.Error("Error: Could not find <Fa> element in the XML.");
            return 1;
        }

        string? p1 = fa.Element(ns + "P_1")?.Value;
        string? p2 = fa.Element(ns + "P_2")?.Value;
        if (string.IsNullOrEmpty(p1) || string.IsNullOrEmpty(p2)) {
            Log.Error("Error: Could not find P_1 (issue date) or P_2 (invoice number) in the XML.");
            return 1;
        }

        fa.Element(ns + "RodzajFaktury")?.SetValue("KOR");
        fa.Element(ns + "P_2")?.SetValue($"FK/{p2}");

        XElement daneFaKorygowanej = new XElement(ns + "DaneFaKorygowanej",
            new XElement(ns + "DataWystFaKorygowanej", p1),
            new XElement(ns + "NrFaKorygowanej", p2)
        );

        XElement? p15Element = fa.Element(ns + "P_15");
        p15Element?.AddAfterSelf(daneFaKorygowanej);

        daneFaKorygowanej.AddAfterSelf(new XElement(ns + "PrzyczynaKorekty", PrzyczynaKorekty));

        if (!string.IsNullOrEmpty(TypKorekty)) {
            fa.Element(ns + "PrzyczynaKorekty")?.AddAfterSelf(new XElement(ns + "TypKorekty", TypKorekty));
        }

        List<string> korektyList = new List<string>(Korekty);
        if (korektyList.Count % 2 != 0) {
            Log.Error("Error: Corrections must be provided in pairs: <numer_lub_nazwa> <ilosc_lub_roznica>");
            return 1;
        }

        Dictionary<string, string> corrections = new Dictionary<string, string>();
        for (int i = 0; i < korektyList.Count; i += 2) {
            corrections[korektyList[i]] = korektyList[i + 1];
        }

        List<XElement> originalWiersze = fa.Elements(ns + "FaWiersz").ToList();
        List<XElement> newWiersze = new List<XElement>();

        foreach (XElement? wiersz in originalWiersze) {
            string? nrWiersza = wiersz.Element(ns + "NrWierszaFa")?.Value;
            string? nazwa = wiersz.Element(ns + "P_7")?.Value;

            if ((nrWiersza != null && corrections.TryGetValue(nrWiersza, out string? zmiana)) ||
                (nazwa != null && corrections.TryGetValue(nazwa, out zmiana))) {
                XElement wierszPrzed = new XElement(wiersz);
                NegateWierszValues(wierszPrzed, ns);
                newWiersze.Add(wierszPrzed);

                XElement wierszPo = new XElement(wiersz);
                ApplyCorrection(wierszPo, ns, zmiana);
                newWiersze.Add(wierszPo);
            } else {
                newWiersze.Add(new XElement(wiersz));
            }
        }

        originalWiersze.Remove();
        fa.Add(newWiersze);

        int wierszId = 1;
        foreach (XElement wiersz in fa.Elements(ns + "FaWiersz")) {
            wiersz.Element(ns + "NrWierszaFa")?.SetValue(wierszId++);
        }

        RecalculateTotals(fa, ns);

        doc = MyXml.Normalize(doc);
        string newXml = MyXml.XmlToString(doc);

        if (!NoValidate) {
            if (XmlValidator.Validate(newXml, out List<string>? errors)) {
                Log.Information("Post-modification validation successful.");
            } else {
                Log.Error("Post-modification validation failed:");
                foreach (string error in errors) {
                    Log.Error(error);
                }
                return 1;
            }
        }

        File.WriteAllText(OutputFile, newXml);
        Log.Information($"Successfully created correction and saved to: {OutputFile}");

        return 0;
    }

    private void NegateWierszValues(XElement wiersz, XNamespace ns) {
        NegateElementValue(wiersz, ns, "P_8B");
        NegateElementValue(wiersz, ns, "P_11");
    }

    private void ApplyCorrection(XElement wiersz, XNamespace ns, string zmiana) {
        XElement? p8bElement = wiersz.Element(ns + "P_8B");
        XElement? p9aElement = wiersz.Element(ns + "P_9A");
        XElement? p11Element = wiersz.Element(ns + "P_11");

        if (p8bElement == null || p9aElement == null || p11Element == null) {
            return;
        }

        decimal originalQty = decimal.Parse(p8bElement.Value, CultureInfo.InvariantCulture);
        decimal unitPrice = decimal.Parse(p9aElement.Value, CultureInfo.InvariantCulture);
        decimal newQty;

        if (zmiana.StartsWith("+") || zmiana.StartsWith("-")) {
            decimal diff = decimal.Parse(zmiana, CultureInfo.InvariantCulture);
            newQty = originalQty + diff;
        } else {
            newQty = decimal.Parse(zmiana, CultureInfo.InvariantCulture);
        }

        p8bElement.Value = newQty.ToString("F2", CultureInfo.InvariantCulture);
        p11Element.Value = (newQty * unitPrice).ToString("F2", CultureInfo.InvariantCulture);
    }

    private void NegateElementValue(XElement wiersz, XNamespace ns, string elementName) {
        XElement? element = wiersz.Element(ns + elementName);
        if (element != null && decimal.TryParse(element.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value)) {
            element.Value = (-value).ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    private void RecalculateTotals(XElement fa, XNamespace ns) {
        Dictionary<string, (decimal Netto, decimal Vat)> summary = new Dictionary<string, (decimal Netto, decimal Vat)>();

        foreach (XElement wiersz in fa.Elements(ns + "FaWiersz")) {
            string? stawkaVat = wiersz.Element(ns + "P_12")?.Value;
            if (stawkaVat == null) {
                continue;
            }

            decimal netto = decimal.Parse(wiersz.Element(ns + "P_11")?.Value ?? "0", CultureInfo.InvariantCulture);

            decimal vatRateNumeric = 0;
            if (decimal.TryParse(stawkaVat, out decimal rate)) {
                vatRateNumeric = rate / 100;
            }

            decimal kwotaVat = netto * vatRateNumeric;

            if (!summary.ContainsKey(stawkaVat)) {
                summary[stawkaVat] = (0, 0);
            }

            summary[stawkaVat] = (summary[stawkaVat].Netto + netto, summary[stawkaVat].Vat + kwotaVat);
        }

        // This is a simplified implementation. A full implementation would need to map
        // VAT rates ("23", "8", "zw", etc.) to the correct P_13_x and P_14_x elements.
        // For now, we only handle a few common rates.

        XElement? p13_1 = fa.Element(ns + "P_13_1");
        XElement? p14_1 = fa.Element(ns + "P_14_1");
        if (p13_1 != null && p14_1 != null && summary.TryGetValue("23", out (decimal Netto, decimal Vat) sums23)) {
            p13_1.Value = sums23.Netto.ToString("F2", CultureInfo.InvariantCulture);
            p14_1.Value = sums23.Vat.ToString("F2", CultureInfo.InvariantCulture);
        }

        XElement? p15 = fa.Element(ns + "P_15");
        if (p15 != null) {
            decimal totalNetto = summary.Values.Sum(v => v.Netto);
            decimal totalVat = summary.Values.Sum(v => v.Vat);
            p15.Value = (totalNetto + totalVat).ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}
