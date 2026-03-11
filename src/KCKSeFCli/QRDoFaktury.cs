using CommandLine;

using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("QRDoFaktury", HelpText = "Generate a QR code for an invoice and save it to a file")]
public class QRDoFakturyCommand : IWithConfigCommand {
    [Value(0, Required = true, HelpText = "KSeF invoice number")]
    public string KsefNumber { get; set; }

    [Value(1, Required = true, HelpText = "Output file path for the QR code (e.g., invoice.jpg)")]
    public string OutputPath { get; set; }

    [Option('p', "pixels", Default = 5, HelpText = "Pixels per module for the QR code")]
    public int PixelsPerModule { get; set; }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        IVerificationLinkService linkSvc = scope.ServiceProvider.GetRequiredService<IVerificationLinkService>();

        string accessToken = await GetAccessToken(scope, cancellationToken).ConfigureAwait(false);
        string invoiceXml = await ksefClient.GetInvoiceAsync(KsefNumber, accessToken, cancellationToken).ConfigureAwait(false);

        string url = LinkDoFakturyCommand.LinkDoFaktury(invoiceXml, linkSvc);

        byte[] qrCodeBytes = QrCodeService.GenerateQrCode(url, PixelsPerModule);

        File.WriteAllBytes(OutputPath, qrCodeBytes);

        Console.WriteLine($"QR code saved to {OutputPath}");

        return 0;
    }
}
