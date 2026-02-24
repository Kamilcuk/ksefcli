using System.Text.Json;

using CommandLine;

using KSeF.Client.Core.Models.Authorization;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("TestTokenAuth", HelpText = "Authenticate using a KSeF token")]
public class TestTokenAuthCommand : IWithConfigCommand {
    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        AuthenticationOperationStatusResponse tokenResponse = await Authenticate.TokenAuth(Config(), scope, GetCryptographicService, cancellationToken).ConfigureAwait(false);
        Console.Out.WriteLine(JsonSerializer.Serialize(tokenResponse));
        return 0;
    }
}
