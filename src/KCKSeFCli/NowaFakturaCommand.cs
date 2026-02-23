// XML KSeF file may not have a namespace.
using System.Globalization;
using System.Xml.Linq;
using CommandLine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.Json;
using System.Text;
using System.Xml;

namespace KCKSeFCli;

[Verb("NowaFaktura", HelpText = "Create a new KSeF XML invoice from a YAML specification.")]
public class NowaFakturaCommand : IGlobalCommand
{
    [Value(0, Required = true, HelpText = "Input YAML specification file path.")]
    public required string InputFile { get; set; }

    [Value(1, Required = true, HelpText = "Output XML file path.")]
    public required string OutputFile { get; set; }

    [Option("bez-walidacji", Required = false, HelpText = "Skip XML validation after creation.")]
    public bool BezWalidacji { get; set; }

    public class InvoiceSpec
    {
        public SellerSpec Sprzedawca { get; set; } = new();
        public BuyerSpec Kupujący { get; set; } = new();
        public List<PositionSpec> Pozycje { get; set; } = new();
        public List<DodatkowyOpisSpec> DodatkowyOpis { get; set; } = new();
        public string? Stopka { get; set; }
        public string? MiejsceWystawieniaFaktury { get; set; }
        public string? DataWykonania { get; set; }
    }

    public class DodatkowyOpisSpec
    {
        public string Klucz { get; set; } = "";
        public string Wartosc { get; set; } = "";
    }

    public abstract class PodmiotSpec
    {
        public string Nip { get; set; } = "";
        public string? NrID { get; set; }
        public string Nazwa { get; set; } = "";
        public string Kraj { get; set; } = "PL";
        public string Adres { get; set; } = "";
        public string? Regon { get; set; }
        public string? PelnaNazwa { get; set; }
        public string? Bdo { get; set; }

        public async Task FillFromNipInfo(string searchDate, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Nip) || (!string.IsNullOrEmpty(Nazwa) && !string.IsNullOrEmpty(Adres) && !string.IsNullOrEmpty(Regon)))
            {
                return; // Only proceed if NIP is present and other details are missing
            }
            
            try
            {
                var nipInfo = await PobierzInfoONipCommand.GetNipDetailsAsync(Nip, searchDate, cancellationToken).ConfigureAwait(false);
                if (nipInfo != null)
                {
                    if (string.IsNullOrEmpty(Nazwa)) Nazwa = nipInfo.Name ?? "";
                    if (string.IsNullOrEmpty(Adres)) Adres = nipInfo.Address ?? "";
                    if (string.IsNullOrEmpty(Regon)) Regon = nipInfo.Regon;
                    // PelnaNazwa is not directly available from the current NIP API response, so we don't fill it for now
                }
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Warning: Could not fetch NIP info for {Nip}: {ex.Message}");
            }
        }
    }

    public class SellerSpec : PodmiotSpec
    {
    }

    public class BuyerSpec : PodmiotSpec
    {
        public int JST { get; set; } = 2;
        public int GV { get; set; } = 2;
    }

    public class PositionSpec
    {
        public string Nazwa { get; set; } = "";
        public string? Jednostka { get; set; } = "";
        public decimal? Ilosc { get; set; } = null;
        public string? StawkaPodatku { get; set; } = null;
        public decimal WartoscBrutto { get; set; }
    }

    private class RateTotals
    {
        public decimal TotalNet { get; set; }
        public decimal TotalVat { get; set; }
    }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        ConfigureLogging();

        if (!File.Exists(InputFile))
        {
            Console.Error.WriteLine($"Error: Input file not found: {InputFile}");
            return 1;
        }

        var yamlContent = await File.ReadAllTextAsync(InputFile, cancellationToken).ConfigureAwait(false);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        var spec = deserializer.Deserialize<InvoiceSpec>(yamlContent);

        var searchDate = DateTime.Now.ToString("yyyy-MM-dd");

        await spec.Sprzedawca.FillFromNipInfo(searchDate, cancellationToken).ConfigureAwait(false);
        await spec.Kupujący.FillFromNipInfo(searchDate, cancellationToken).ConfigureAwait(false);

        var xml = GenerateXml(spec);
        await File.WriteAllTextAsync(OutputFile, xml, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Successfully created invoice and saved to: {OutputFile}");

        if (!BezWalidacji)
        {
            if (XmlValidator.Validate(xml, out var errors))
            {
                Console.WriteLine("Validation successful.");
            }
            else
            {
                Console.Error.WriteLine("Validation failed:");
                foreach (var error in errors)
                {
                    Console.Error.WriteLine(error);
                }
                return 1;
            }
        }

        return 0;
    }

    private List<XElement> CreatePodmiotElements(PodmiotSpec podmiot)
    {
        var elements = new List<XElement>();

        string name = podmiot.Nazwa;
        string address = podmiot.Adres;
        string nip = podmiot.Nip?.Replace("-", "") ?? "";

        var daneIdentyfikacyjne = new XElement("DaneIdentyfikacyjne");
        if (!string.IsNullOrEmpty(nip))
        {
            daneIdentyfikacyjne.Add(new XElement("NIP", nip));
        }
        else if (!string.IsNullOrEmpty(podmiot.NrID))
        {
            daneIdentyfikacyjne.Add(new XElement("NrID", podmiot.NrID));
        }
        else
        {
            daneIdentyfikacyjne.Add(new XElement("BrakID", "1"));
        }
        daneIdentyfikacyjne.Add(new XElement("Nazwa", string.IsNullOrEmpty(name) ? "BRAK" : name));
        
        elements.Add(daneIdentyfikacyjne);

        elements.Add(new XElement("Adres",
            new XElement("KodKraju", podmiot.Kraj),
            new XElement("AdresL1", string.IsNullOrEmpty(address) ? "BRAK" : address)
        ));
        elements.Add(new XElement("AdresKoresp",
            new XElement("KodKraju", "PL"),
            new XElement("AdresL1", "BRAK")
        ));

        if (podmiot is BuyerSpec buyer)
        {
            elements.Add(new XElement("NrKlienta", "BRAK"));
            elements.Add(new XElement("IDNabywcy", "BRAK"));
            elements.Add(new XElement("JST", "1")); 
            elements.Add(new XElement("GV", "1"));
        }

        return elements;
    }

    private string GenerateXml(InvoiceSpec spec)
    {
        XNamespace ns = "http://crd.gov.pl/wzor/2025/06/25/13775/";
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

        var now = DateTime.UtcNow;
        
        var totalsByRate = new Dictionary<string, RateTotals>();
        decimal totalGross = 0;
        bool hasOO = false;

        var faWiersze = new List<XElement>();
        for (int i = 0; i < spec.Pozycje.Count; i++)
        {
            var p = spec.Pozycje[i];
            string rate = (p.StawkaPodatku ?? "23").Replace("%", "");
            if (rate.ToLower() == "odwrotne obciążenie") rate = "oo";
            if (rate == "oo") hasOO = true;
            
            decimal vatRate = 0;
            if (decimal.TryParse(rate, out decimal parsedRate))
            {
                vatRate = parsedRate / 100m;
            }

            decimal net = Math.Round(p.WartoscBrutto / (1 + vatRate), 2);
            decimal vat = p.WartoscBrutto - net;

            totalGross += p.WartoscBrutto;
            
            if (!totalsByRate.ContainsKey(rate))
            {
                totalsByRate[rate] = new RateTotals();
            }
            totalsByRate[rate].TotalNet += net;
            totalsByRate[rate].TotalVat += vat;

            var faWiersz = new XElement("FaWiersz",
                new XElement("NrWierszaFa", (i + 1).ToString()),
                new XElement("P_7", p.Nazwa));

            if (!string.IsNullOrEmpty(p.Jednostka))
            {
                faWiersz.Add(new XElement("P_8A", p.Jednostka));
            }

            if (p.Ilosc.HasValue)
            {
                faWiersz.Add(new XElement("P_8B", p.Ilosc.Value.ToString("F2", CultureInfo.InvariantCulture)));
            }

            faWiersz.Add(
                new XElement("P_9A", net.ToString("F2", CultureInfo.InvariantCulture)),
                new XElement("P_11", net.ToString("F2", CultureInfo.InvariantCulture)),
                new XElement("P_12", rate)
            );

            faWiersze.Add(faWiersz);
        }

        var faElements = new List<XElement>
        {
            new XElement("KodWaluty", "PLN"),
            new XElement("P_1", now.ToString("yyyy-MM-dd")),
        };

        if (!string.IsNullOrEmpty(spec.MiejsceWystawieniaFaktury))
        {
            faElements.Add(new XElement("P_1M", spec.MiejsceWystawieniaFaktury));
        }

        string dataWykonania = spec.DataWykonania ?? now.ToString("yyyy-MM-dd");

        faElements.AddRange(new XElement[]
        {
            new XElement("P_2", "FV/" + now.ToString("yyyyMMdd") + "/01"),
            new XElement("P_6", dataWykonania) 
        });

        if (totalsByRate.TryGetValue("23", out var totals23) && totals23.TotalNet > 0)
        {
            faElements.Add(new XElement("P_13_1", totals23.TotalNet.ToString("F2", CultureInfo.InvariantCulture)));
            faElements.Add(new XElement("P_14_1", totals23.TotalVat.ToString("F2", CultureInfo.InvariantCulture)));
        }
        if (totalsByRate.TryGetValue("8", out var totals8) && totals8.TotalNet > 0)
        {
            faElements.Add(new XElement("P_13_2", totals8.TotalNet.ToString("F2", CultureInfo.InvariantCulture)));
            faElements.Add(new XElement("P_14_2", totals8.TotalVat.ToString("F2", CultureInfo.InvariantCulture)));
        }
        if (totalsByRate.TryGetValue("5", out var totals5) && totals5.TotalNet > 0)
        {
            faElements.Add(new XElement("P_13_3", totals5.TotalNet.ToString("F2", CultureInfo.InvariantCulture)));
            faElements.Add(new XElement("P_14_3", totals5.TotalVat.ToString("F2", CultureInfo.InvariantCulture)));
        }

        faElements.Add(new XElement("P_15", totalGross.ToString("F2", CultureInfo.InvariantCulture)));
        
        // Adnotacje section - Strictly matching user's example
        faElements.Add(new XElement("Adnotacje",
            new XElement("P_16", "2"),
            new XElement("P_17", "2"),
            new XElement("P_18", hasOO ? "1" : "2"),
            new XElement("P_18A", "2"),
            new XElement("Zwolnienie",
                new XElement("P_19N", "1")
            ),
            new XElement("NoweSrodkiTransportu",
                new XElement("P_22N", "1")
            ),
            new XElement("P_23", "2"),
            new XElement("PMarzy",
                new XElement("P_PMarzyN", "1")
            )
        ));
        
        faElements.Add(new XElement("RodzajFaktury", "VAT"));

        foreach (var opis in spec.DodatkowyOpis)
        {
            faElements.Add(new XElement("DodatkowyOpis",
                new XElement("Klucz", opis.Klucz),
                new XElement("Wartosc", opis.Wartosc)
            ));
        }

        foreach (var wiersz in faWiersze)
        {
            faElements.Add(wiersz);
        }

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("Faktura",
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute("xmlns", ns.NamespaceName), // Set default namespace here
                new XAttribute(xsi + "schemaLocation", ns.NamespaceName),
                new XElement("Naglowek",
                    new XElement("KodFormularza",
                        new XAttribute("kodSystemowy", "FA (3)"),
                        new XAttribute("wersjaSchemy", "1-0E"),
                        "FA"),
                    new XElement("WariantFormularza", "3"),
                    new XElement("DataWytworzeniaFa", now.ToString("yyyy-MM-ddTHH:mm:ssZ")), // Use local time string for schema
                    new XElement("SystemInfo", "KCKSeFCli")
                ),
                new XElement("Podmiot1", CreatePodmiotElements(spec.Sprzedawca)),
                new XElement("Podmiot2", CreatePodmiotElements(spec.Kupujący)),
                new XElement("Fa", faElements)
            )
        );

        foreach (var el in doc.Descendants())
    {
            SetDefaultXmlNamespace(el, ns);
    }

using var ms = new MemoryStream();
// Use UTF8 without BOM to avoid "Data at the root level is invalid" parsing errors in XmlReader
var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
using (var writer = XmlWriter.Create(ms, settings))
{
    doc.Save(writer);
}
string xml = Encoding.UTF8.GetString(ms.ToArray());

        return xml;
    }

public static void SetDefaultXmlNamespace( XElement xelem, XNamespace xmlns)
{
    if(xelem.Name.NamespaceName == string.Empty)
        xelem.Name = xmlns + xelem.Name.LocalName;
    foreach(var e in xelem.Elements())
        SetDefaultXmlNamespace(e, xmlns);
}

public static XElement WithDefaultXmlNamespace( XElement xelem, XNamespace xmlns)
{
    XName name;
    if(xelem.Name.NamespaceName == string.Empty)
        name = xmlns + xelem.Name.LocalName;
    else
        name = xelem.Name;
    return new XElement(name,
                    from e in xelem.Elements()
                    select WithDefaultXmlNamespace(e, xmlns));
}


}
