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
    [Option('o', "outputdir", HelpText = "Output directory to save files to (default: current directory).")]
    public string? OutputDir { get; set; }

    [Option("useInvoiceNumber", HelpText = "Use InvoiceNumber instead of KsefNumber for the filename to save invoices.")]
    public bool UseInvoiceNumber { get; set; }

    [Option("no-summary", HelpText = "Nie zapisuj metadanych faktury w plikach _summary.json")]
    public bool NoSummary { get; set; }

    [Option("retry-attempts", Default = 5, HelpText = "Number of retry attempts on rate limit.")]
    public int RetryAttempts { get; set; }

    [Option("no-local-rate-limit", HelpText = "Disable local rate limiting.")]
    public bool NoLocalRateLimit { get; set; }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        string outputDir = OutputDir ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(outputDir);

        IVerificationLinkService linkSvc = scope.ServiceProvider.GetRequiredService<IVerificationLinkService>();
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();

        List<InvoiceSummary> invoices = await base.SzukajFaktury(scope, ksefClient, cancellationToken).ConfigureAwait(false);

        foreach (InvoiceSummary invoiceSummary in invoices) {
            string fileName = UseInvoiceNumber ? invoiceSummary.InvoiceNumber : invoiceSummary.KsefNumber;
            // Replace path separators to prevent creating subdirectories
            fileName = fileName.Replace('/', '_').Replace('\\', '_');
            string summaryJsonFilePath = Path.Combine(outputDir, $"{fileName}_summary.json");
            string xmlFilePath = Path.Combine(outputDir, $"{fileName}.xml");

            if (!NoSummary) {
                File.WriteAllText(summaryJsonFilePath, JsonSerializer.Serialize(invoiceSummary));
                Log.Information($"Saved invoice {invoiceSummary.KsefNumber} to {summaryJsonFilePath}");
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
        }

        return 0;
    }

}
