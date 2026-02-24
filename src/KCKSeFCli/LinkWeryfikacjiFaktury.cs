using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;

using CommandLine;

using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace KCKSeFCli;

[Verb("LinkWeryfikacjiFaktury", HelpText = "Generuje link weryfikacji faktury (KOD II).")]
public class LinkWeryfikacjiFaktury : IWithConfigCommand {
    [Value(0, Required = true, HelpText = "Plik XML z fakturą.")]
    public string FilePath { get; set; }

    public static string GenerateCertificateVerificationLink(string invoiceXml, IVerificationLinkService linkSvc, X509Certificate2 certificate) {
        XDocument xmlDoc = XDocument.Parse(invoiceXml);
        if (xmlDoc.Root is null) {
            throw new InvalidDataException("Invoice XML is missing the root element.");
        }

        XElement podmiot1 = xmlDoc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Podmiot1") ?? throw new InvalidDataException("Could not find Podmiot1 in invoice XML.");

        XNamespace ns = podmiot1.Name.Namespace;

        string sellerNip = podmiot1.Element(ns + "DaneIdentyfikacyjne")?.Element(ns + "NIP")?.Value ?? throw new InvalidDataException("Could not find seller NIP in invoice XML.");

        byte[] invoiceBytes = Encoding.UTF8.GetBytes(invoiceXml);
        byte[] hashBytes = SHA256.HashData(invoiceBytes);
        string invoiceHash = Base64UrlEncoder.Encode(hashBytes);

        string url = linkSvc.BuildCertificateVerificationUrl(
            sellerNip,
            KSeF.Client.Core.Models.QRCode.QRCodeContextIdentifierType.Nip,
            sellerNip,
            invoiceHash,
            certificate);
        return url;
    }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        IVerificationLinkService linkSvc = scope.ServiceProvider.GetRequiredService<IVerificationLinkService>();

        ProfileConfigWithName config = Config();
        if (config.Certificate is null || string.IsNullOrEmpty(config.Certificate.Certificate)) {
            throw new InvalidOperationException("Certificate is not configured for this profile.");
        }
        byte[] certBytes = Encoding.UTF8.GetBytes(config.Certificate.Certificate!);
        X509Certificate2 publicCert = certBytes.LoadCertificate();
        X509Certificate2 certificate = publicCert.MergeWithPemKey(config.Certificate.Private_Key!, config.Certificate.Password ?? string.Empty);

        string invoiceXml = await File.ReadAllTextAsync(FilePath, cancellationToken).ConfigureAwait(false);

        string url = GenerateCertificateVerificationLink(invoiceXml, linkSvc, certificate);

        Console.WriteLine(url);

        return 0;
    }
}
