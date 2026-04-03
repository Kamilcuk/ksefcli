using System.Text.Json;
using System.Xml.Linq;

using CommandLine;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Invoices;
using KCKSeFCli.Utils;

using Microsoft.Extensions.DependencyInjection;

using KCKSeFCli;

namespace KCKSeFCli;

[Verb("PobierzFaktury", HelpText = "Download invoices based on search criteria.")]
public class PobierzFakturyCommand : SzukajFakturCommand {
    [Option('o', "outputdir", Required = true, HelpText = "Output directory to save files to.")]
    public required string OutputDir { get; set; }

    [Option('p', "pdf", HelpText = "Save also pdf files.")]
    public bool Pdf { get; set; }

    [Option("useInvoiceNumber", HelpText = "Use InvoiceNumber instead of KsefNumber for the filename to save invoices.")]
    public bool UseInvoiceNumber { get; set; }

    [Option("no-json", HelpText = "Nie zapisuj metadanych faktury w plikach .json")]
    public bool NoJson { get; set; }

    [Option("retry-attempts", Default = 5, HelpText = "Number of retry attempts on rate limit.")]
    public int RetryAttempts { get; set; }

    [Option("no-local-rate-limit", HelpText = "Disable local rate limiting.")]
    public bool NoLocalRateLimit { get; set; }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        // IMPORTANT: We must initialize the PDF runner before any network operations with KSeF.
        // If the environment is missing dependencies for SkiaSharp or font resolution,
        // we should fail fast before potentially consuming KSeF API limits.
        XML2PDFCommand.Runner? pdfRunner = null;
        if (Pdf) {
            pdfRunner = await XML2PDFCommand.GetRunner(cancellationToken).ConfigureAwait(false);
        }

        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();

        List<InvoiceSummary> invoices = await base.SzukajFaktury(scope, ksefClient, cancellationToken).ConfigureAwait(false);

        await ProcessInvoices(scope, invoices, pdfRunner, cancellationToken).ConfigureAwait(false);

        return 0;
    }

    protected async Task ProcessInvoices(IServiceScope scope, List<InvoiceSummary> invoices, XML2PDFCommand.Runner? pdfRunner, CancellationToken cancellationToken) {
        Directory.CreateDirectory(OutputDir);

        IVerificationLinkService linkSvc = scope.ServiceProvider.GetRequiredService<IVerificationLinkService>();
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();

        foreach (InvoiceSummary invoiceSummary in invoices) {
            string fileName = UseInvoiceNumber ? invoiceSummary.InvoiceNumber : invoiceSummary.KsefNumber;
            string jsonFilePath = Path.Combine(OutputDir, $"{fileName}.json");
            string xmlFilePath = Path.Combine(OutputDir, $"{fileName}.xml");

            if (!NoJson) {
                File.WriteAllText(jsonFilePath, JsonSerializer.Serialize(invoiceSummary));
                Log.Information($"Saved invoice {invoiceSummary.KsefNumber} to {jsonFilePath}");
            }

            string accessToken = await GetAccessToken(scope, cancellationToken).ConfigureAwait(false);

            ILimitsClient? limitsClient = NoLocalRateLimit ? null : scope.ServiceProvider.GetRequiredService<ILimitsClient>();
            string invoiceXml = await KsefRateLimitWrapper.ExecuteWithRetryAsync(
                (ct) => ksefClient.GetInvoiceAsync(invoiceSummary.KsefNumber, accessToken, ct),
                KsefApiEndpoint.InvoiceGetByNumber,
                limitsClient,
                RetryAttempts,
                accessToken,
                cancellationToken).ConfigureAwait(false);

            File.WriteAllText(xmlFilePath, XDocument.Parse(invoiceXml).ToString() + "\n");

            Log.Information($"Saved invoice {invoiceSummary.KsefNumber} to {xmlFilePath}");

            if (Pdf) {
                string qrCodeUrl = LinkDoFakturyCommand.LinkDoFaktury(invoiceXml, linkSvc);
                byte[] pdfContent = await pdfRunner!.XML2PDF(invoiceXml, Quiet, false, invoiceSummary.KsefNumber, qrCodeUrl, null, cancellationToken).ConfigureAwait(false);
                string outputPdfPath = Path.ChangeExtension(xmlFilePath, ".pdf");
                File.WriteAllBytes(outputPdfPath, pdfContent);
                Log.Information($"Saved PDF for {xmlFilePath} to {outputPdfPath}");
            }
        }
    }

}
