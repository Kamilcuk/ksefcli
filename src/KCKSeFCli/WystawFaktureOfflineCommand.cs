using System.Security.Cryptography.X509Certificates;
using System.Text;

using CommandLine;

using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Extensions;

using Microsoft.Extensions.DependencyInjection;

using KCKSeFCli;

namespace KCKSeFCli;

[Verb("WystawFaktureOffline", HelpText = "Convert KSeF XML invoice to PDF, adding an offline verification QR code (KOD II).")]
public class WystawFaktureOfflineCommand : IWithConfigCommand {
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    [Value(1, HelpText = "Output PDF file path.")]
    public string? OutputFile { get; set; }

    [Option("nrKSeF", Required = false, HelpText = "KSeF invoice number to embed in PDF.")]
    public string? NrKSeF { get; set; }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        if (!File.Exists(InputFile)) {
            Console.Error.WriteLine($"Error: Input file not found: {InputFile}");
            return 1;
        }

        string outputPdfPath;
        if (string.IsNullOrEmpty(OutputFile)) {
            if (!InputFile.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) {
                Console.Error.WriteLine("Error: Input file must have a .xml extension when no output file is specified.");
                return 1;
            }
            outputPdfPath = Path.ChangeExtension(InputFile, ".pdf")!;
            if (File.Exists(outputPdfPath)) {
                Console.Error.WriteLine($"Error: Output file already exists: {outputPdfPath}");
                return 1;
            }
        } else {
            outputPdfPath = OutputFile!;
        }

        ProfileConfigWithName config = Config();
        if (config.Certificate is null || string.IsNullOrEmpty(config.Certificate.Certificate)) {
            throw new InvalidOperationException("Certificate is not configured for this profile.");
        }

        IVerificationLinkService linkSvc = scope.ServiceProvider.GetRequiredService<IVerificationLinkService>();
        string xmlContent = File.ReadAllText(InputFile);

        Log.Debug("--- Generate KOD I QR Code ---");
        string invoiceUrl = LinkDoFakturyCommand.LinkDoFaktury(xmlContent, linkSvc);

        Log.Debug("--- Generate KOD II QR Code ---");
        byte[] certBytes = Encoding.UTF8.GetBytes(config.Certificate.Certificate!);
        X509Certificate2 publicCert = certBytes.LoadCertificate();
        X509Certificate2 certificate = publicCert.MergeWithPemKey(config.Certificate.Private_Key!, config.Certificate.Password ?? string.Empty);
        string verificationUrl = LinkWeryfikacjiFaktury.GenerateCertificateVerificationLink(xmlContent, linkSvc, certificate);

        Log.Debug("Converting to PDF");
        XML2PDFCommand.Runner runner = await XML2PDFCommand.GetRunner(cancellationToken).ConfigureAwait(false);
        byte[] pdfContent = await runner.XML2PDF(xmlContent, Quiet, false, NrKSeF ?? " ", invoiceUrl, verificationUrl, cancellationToken).ConfigureAwait(false);

        File.WriteAllBytes(outputPdfPath, pdfContent);

        Console.WriteLine($"PDF saved to: {outputPdfPath}");

        return 0;
    }
}
