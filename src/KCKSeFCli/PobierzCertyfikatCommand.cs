using System.Text.Json;

using CommandLine;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Certificates;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("PobierzCertyfikat", HelpText = "Retrieve KSeF certificate content by serial number.")]
public class PobierzCertyfikatCommand : IWithConfigCommand {
    [Value(0, Required = true, HelpText = "Certificate serial number to retrieve.")]
    public string CertificateSerialNumber { get; set; }

    [Option('o', "outputFile", HelpText = "Output file path to save the certificate.")]
    public string? OutputFile { get; set; }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        string accessToken = await GetAccessToken(scope, cancellationToken).ConfigureAwait(false);

        CertificateListRequest request = new CertificateListRequest { CertificateSerialNumbers = new[] { CertificateSerialNumber } };
        CertificateListResponse certificateListResponse = await ksefClient.GetCertificateListAsync(request, accessToken, cancellationToken).ConfigureAwait(false);

        CertificateResponse? certificate = certificateListResponse.Certificates.FirstOrDefault();

        if (certificate == null) {
            Console.Error.WriteLine($"Error: Certificate with serial number {CertificateSerialNumber} not found.");
            return 1;
        }

        if (string.IsNullOrEmpty(OutputFile)) {
            Console.WriteLine(JsonSerializer.Serialize(certificate, new JsonSerializerOptions { WriteIndented = true }));
        } else {
            File.WriteAllText(OutputFile!, certificate.Certificate);
            Console.WriteLine($"Certificate saved to {OutputFile}");
        }

        return 0;
    }
}
