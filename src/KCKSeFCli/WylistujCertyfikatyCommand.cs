using System.Text.Json;

using CommandLine;

using KSeF.Client.Api.Builders.Certificates;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Certificates;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("WylistujCertyfikaty", HelpText = "List KSeF certificate metadata.")]
public class WylistujCertyfikatyCommand : IWithConfigCommand {
    [Option("name", HelpText = "Filter by certificate name.")]
    public string? Name { get; set; }

    [Option("serialNumber", HelpText = "Filter by certificate serial number.")]
    public string? CertificateSerialNumber { get; set; }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        string accessToken = await GetAccessToken(scope, cancellationToken).ConfigureAwait(false);

        IGetCertificateMetadataListListRequestBuilder requestBuilder = KSeF.Client.Api.Builders.Certificates.GetCertificateMetadataListRequestBuilder.Create();

        if (!string.IsNullOrEmpty(Name)) {
            requestBuilder.WithName(Name);
        }

        if (!string.IsNullOrEmpty(CertificateSerialNumber)) {
            requestBuilder.WithCertificateSerialNumber(CertificateSerialNumber);
        }

        CertificateMetadataListRequest request = requestBuilder.Build();

        CertificateMetadataListResponse certificateListResponse = await ksefClient.GetCertificateMetadataListAsync(
            accessToken,
            request,
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        Console.WriteLine(JsonSerializer.Serialize(certificateListResponse, new JsonSerializerOptions { WriteIndented = true }));

        return 0;
    }
}
