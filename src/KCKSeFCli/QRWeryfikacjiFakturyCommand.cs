using System.Security.Cryptography.X509Certificates;
using System.Text;

using CommandLine;

using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Extensions;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("QRWeryfikacjiFaktury", HelpText = "Generate a verification QR code (KOD II) for an invoice and save it to a file.")]
public class QRWeryfikacjiFakturyCommand : IWithConfigCommand {
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    [Value(1, Required = true, HelpText = "Output file path for the QR code (e.g., invoice_qr.jpg)")]
    public string OutputPath { get; set; }

    [Option('p', "pixels", Default = 5, HelpText = "Pixels per module for the QR code")]
    public int PixelsPerModule { get; set; }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        IVerificationLinkService linkSvc = scope.ServiceProvider.GetRequiredService<IVerificationLinkService>();

        ProfileConfigWithName config = Config();
        if (config.Certificate is null || string.IsNullOrEmpty(config.Certificate.Certificate)) {
            throw new InvalidOperationException("Certificate is not configured for this profile.");
        }
        byte[] certBytes = Encoding.UTF8.GetBytes(config.Certificate.Certificate!);
        X509Certificate2 publicCert = certBytes.LoadCertificate();
        X509Certificate2 certificate = publicCert.MergeWithPemKey(config.Certificate.Private_Key!, config.Certificate.Password ?? string.Empty);

        string invoiceXml = await File.ReadAllTextAsync(InputFile, cancellationToken).ConfigureAwait(false);

        string url = LinkWeryfikacjiFaktury.GenerateCertificateVerificationLink(invoiceXml, linkSvc, certificate);

        byte[] qrCodeBytes = KSeF.Client.Api.Services.QrCodeService.GenerateQrCode(url, PixelsPerModule);
        // byte[] labeledQrCodeBytes = KSeF.Client.Api.Services.QrCodeService.AddLabelToQrCode(qrCodeBytes, "CERTYFIKAT");

        await File.WriteAllBytesAsync(OutputPath, qrCodeBytes, cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Verification QR code saved to {OutputPath}");

        return 0;
    }
}
