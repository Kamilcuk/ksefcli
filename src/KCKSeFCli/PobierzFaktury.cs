using System.Text.Json;
using System.Xml.Linq;

using CommandLine;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Invoices;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("PobierzFaktury", HelpText = "Download invoices based on search criteria.")]
public class PobierzFakturyCommand : SzukajFakturCommand
{
    [Option('o', "outputdir", Required = true, HelpText = "Output directory to save files to.")]
    public required string OutputDir { get; set; }

    [Option('p', "pdf", HelpText = "Save also pdf files.")]
    public bool Pdf { get; set; }

    [Option("useInvoiceNumber", HelpText = "Use InvoiceNumber instead of KsefNumber for the filename to save invoices.")]
    public bool UseInvoiceNumber { get; set; }

    [Option("zapiszjson", HelpText = "Zapisz metadane faktury w plik .json")]
    public bool ZapiszJson { get; set; }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        XML2PDFCommand.Runner? pdfRunner = null;
        if (Pdf)
        {
            pdfRunner = await XML2PDFCommand.GetRunner(cancellationToken).ConfigureAwait(false);
        }

        Directory.CreateDirectory(OutputDir);

        IVerificationLinkService linkSvc = scope.ServiceProvider.GetRequiredService<IVerificationLinkService>();
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();

        List<InvoiceSummary> invoices = await base.SzukajFaktury(scope, ksefClient, cancellationToken).ConfigureAwait(false);

        foreach (InvoiceSummary invoiceSummary in invoices)
        {
            string fileName = UseInvoiceNumber ? invoiceSummary.InvoiceNumber : invoiceSummary.KsefNumber;
            string jsonFilePath = Path.Combine(OutputDir, $"{fileName}.json");
            string xmlFilePath = Path.Combine(OutputDir, $"{fileName}.xml");

            if (ZapiszJson)
            {
                await File.WriteAllTextAsync(jsonFilePath, JsonSerializer.Serialize(invoiceSummary), cancellationToken).ConfigureAwait(false);
                Log.LogInformation($"Saved invoice {invoiceSummary.KsefNumber} to {jsonFilePath}");
            }

            string accessToken = await GetAccessToken(scope, cancellationToken).ConfigureAwait(false);
            string invoiceXml = await ksefClient.GetInvoiceAsync(invoiceSummary.KsefNumber, accessToken, cancellationToken).ConfigureAwait(false);

            await File.WriteAllTextAsync(xmlFilePath, XDocument.Parse(invoiceXml).ToString() + "\n", cancellationToken).ConfigureAwait(false);

            Log.LogInformation($"Saved invoice {invoiceSummary.KsefNumber} to {xmlFilePath}");

            if (Pdf)
            {
                string qrCodeUrl = LinkDoFakturyCommand.LinkDoFaktury(invoiceXml, linkSvc);
                byte[] pdfContent = await pdfRunner!.XML2PDF(invoiceXml, Quiet, false, invoiceSummary.KsefNumber, qrCodeUrl, cancellationToken).ConfigureAwait(false);
                string outputPdfPath = Path.ChangeExtension(xmlFilePath, ".pdf");
                await File.WriteAllBytesAsync(outputPdfPath, pdfContent, cancellationToken).ConfigureAwait(false);
                Log.LogInformation($"Saved PDF for {xmlFilePath} to {outputPdfPath}");
            }
        }

        return 0;
    }

}
