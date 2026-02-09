using System.Text.Json;

using CommandLine;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Invoices;

using Microsoft.Extensions.DependencyInjection;

namespace KSeFCli;

[Verb("PobierzFaktury", HelpText = "Download invoices based on search criteria.")]
public class PobierzFakturyCommand : SzukajFakturCommand
{
    [Option('o', "outputdir", Required = true, HelpText = "Output directory to save files to.")]
    public required string OutputDir { get; set; }

    [Option('p', "pdf", HelpText = "Save also pdf files.")]
    public bool Pdf { get; set; }

    [Option("useInvoiceNumber", HelpText = "Use InvoiceNumber instead of KsefNumber for the filename to save invoices.")]
    public bool UseInvoiceNumber { get; set; }

    [Option("dodaj-numer-ksef-do-dodatkowego-opisu", HelpText = "Adds KSeF number to the invoice XML in the 'DodatkowyOpis' section.")]
    public bool DodajNumerKsefDoDodatkowegoOpisuFlag { get; set; }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        if (Pdf)
        {
            XML2PDFCommand.AssertNpxExists();
        }

        Directory.CreateDirectory(OutputDir);

        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();

        List<InvoiceSummary> invoices = await base.SzukajFaktury(scope, ksefClient, cancellationToken).ConfigureAwait(false);

        foreach (InvoiceSummary invoiceSummary in invoices)
        {
            string fileName = UseInvoiceNumber ? invoiceSummary.InvoiceNumber : invoiceSummary.KsefNumber;
            string jsonFilePath = Path.Combine(OutputDir, $"{fileName}.json");
            string xmlFilePath = Path.Combine(OutputDir, $"{fileName}.xml");

            await File.WriteAllTextAsync(jsonFilePath, JsonSerializer.Serialize(invoiceSummary), cancellationToken).ConfigureAwait(false);

            string invoiceXml = await ksefClient.GetInvoiceAsync(invoiceSummary.KsefNumber, await GetAccessToken(scope, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            if (DodajNumerKsefDoDodatkowegoOpisuFlag)
            {
                invoiceXml = DodajNumerKsefDoDodatkowegoOpisu(invoiceXml, invoiceSummary.KsefNumber);
            }
            await File.WriteAllTextAsync(xmlFilePath, invoiceXml, cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"Saved invoice {invoiceSummary.KsefNumber} to {xmlFilePath}");

            if (Pdf)
            {
                byte[] pdfContent = await XML2PDFCommand.XML2PDF(invoiceXml, Quiet, cancellationToken).ConfigureAwait(false);
                string outputPdfPath = Path.ChangeExtension(xmlFilePath, ".pdf");
                await File.WriteAllBytesAsync(outputPdfPath, pdfContent, cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"Saved PDF for {xmlFilePath} to {outputPdfPath}");
            }
        }

        return 0;
    }

    public static string DodajNumerKsefDoDodatkowegoOpisu(string invoiceXml, string ksefNumber)
    {
        var xml = System.Xml.Linq.XDocument.Parse(invoiceXml);
        if (xml.Root is null)
            throw new InvalidOperationException("XML root element not found.");

        var ns = xml.Root.GetDefaultNamespace();

        var faElement = xml.Root.Element(ns + "Fa");
        if (faElement is null)
            throw new InvalidOperationException("Element <Fa> not found in invoice XML.");

        var faWiersz = faElement.Element(ns + "FaWiersz");
        if (faWiersz is null)
            throw new InvalidOperationException("Element <FaWiersz> not found in invoice XML.");

        var dodatkowyOpis = new System.Xml.Linq.XElement(ns + "DodatkowyOpis",
            new System.Xml.Linq.XElement(ns + "Klucz", "Numer faktury KSEF"),
            new System.Xml.Linq.XElement(ns + "Wartosc", ksefNumber)
        );

        faWiersz.AddBeforeSelf(dodatkowyOpis);

        return xml.ToString();
    }
}
