using System.Text.Json;

using CommandLine;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Certificates;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("SprawdzLimitCertyfikatow", HelpText = "Check available certificate limits.")]
public class SprawdzLimitCertyfikatowCommand : IWithConfigCommand
{
    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        string accessToken = await GetAccessToken(scope, cancellationToken).ConfigureAwait(false);

        CertificateLimitResponse certificateLimitResponse = await ksefClient.GetCertificateLimitsAsync(accessToken, cancellationToken).ConfigureAwait(false);

        Console.WriteLine(JsonSerializer.Serialize(certificateLimitResponse, new JsonSerializerOptions { WriteIndented = true }));

        return 0;
    }
}
