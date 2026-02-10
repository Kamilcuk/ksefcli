using System.Text.Json;

using CommandLine;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Certificates;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("WylistujCertyfikaty", HelpText = "List KSeF certificate metadata.")]
public class WylistujCertyfikatyCommand : IWithConfigCommand
{
    [Option("status", HelpText = "Filter by certificate status (Active, Blocked, Revoked, Expired).")]
    public string? Status { get; set; }

    [Option("expiresAfter", HelpText = "Filter by expiry date (certificates expiring after this date).")]
    public string? ExpiresAfter { get; set; }

    [Option("name", HelpText = "Filter by certificate name.")]
    public string? Name { get; set; }

    [Option("type", HelpText = "Filter by certificate type (Authentication, Offline).")]
    public string? Type { get; set; }

    [Option("serialNumber", HelpText = "Filter by certificate serial number.")]
    public string? CertificateSerialNumber { get; set; }

    [Option("pageOffset", Default = 0, HelpText = "Page offset for pagination.")]
    public int PageOffset { get; set; }

    [Option("pageSize", Default = 10, HelpText = "Page size for pagination.")]
    public int PageSize { get; set; }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        string accessToken = await GetAccessToken(scope, cancellationToken).ConfigureAwait(false);

        var requestBuilder = GetCertificateMetadataListRequestBuilder.Create();

        if (!string.IsNullOrEmpty(Status))
        {
            if (!Enum.TryParse(Status, true, out CertificateStatus status))
            {
                throw new ArgumentException($"Invalid certificate status: {Status}");
            }
            requestBuilder.WithStatus(status);
        }

        if (!string.IsNullOrEmpty(ExpiresAfter))
        {
            DateTime expiresAfterDate = await ParseDate.Parse(ExpiresAfter).ConfigureAwait(false);
            requestBuilder.WithExpiresAfter(expiresAfterDate);
        }

        if (!string.IsNullOrEmpty(Name))
        {
            requestBuilder.WithName(Name);
        }

        if (!string.IsNullOrEmpty(Type))
        {
            if (!Enum.TryParse(Type, true, out CertificateType type))
            {
                throw new ArgumentException($"Invalid certificate type: {Type}");
            }
            requestBuilder.WithType(type);
        }

        if (!string.IsNullOrEmpty(CertificateSerialNumber))
        {
            requestBuilder.WithCertificateSerialNumber(CertificateSerialNumber);
        }

        GetCertificateMetadataListRequest request = requestBuilder.Build();

        CertificateMetadataListResponse certificateListResponse = await ksefClient.GetCertificateMetadataListAsync(
            accessToken,
            request,
            PageSize,
            PageOffset,
            cancellationToken).ConfigureAwait(false);

        Console.WriteLine(JsonSerializer.Serialize(certificateListResponse, new JsonSerializerOptions { WriteIndented = true }));

        return 0;
    }
}
