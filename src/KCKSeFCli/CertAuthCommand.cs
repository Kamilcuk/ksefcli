using System.Text.Json;

using CommandLine;

using KSeF.Client.Core.Models.Authorization;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("CertAuth", HelpText = "Authenticate using a certificate")]
public class CertAuthCommand : IWithConfigCommand
{
    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        AuthenticationOperationStatusResponse tokenResponse = await Authenticate.CertAuth(Config(), scope, GetCryptographicService, cancellationToken).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(tokenResponse));
        return 0;
    }
}
