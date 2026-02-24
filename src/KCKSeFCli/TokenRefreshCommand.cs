using System.Text.Json;

using CommandLine;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Authorization;

using Microsoft.Extensions.DependencyInjection;


namespace KCKSeFCli;

[Verb("TokenRefresh", HelpText = "Refresh an existing session token")]
public class TokenRefreshCommand : IWithConfigCommand {
    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        ProfileConfigWithName config = Config();
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        if (string.IsNullOrEmpty(config.Token)) {
            Console.Error.WriteLine("No refresh token provided. Use --token to provide a refresh token.");
            return 1;
        }
        Log.Information("Refreshing token...");
        RefreshTokenResponse tokenResponse = await ksefClient.RefreshAccessTokenAsync(config.Token, cancellationToken).ConfigureAwait(false);
        Console.Out.WriteLine(JsonSerializer.Serialize(tokenResponse));
        Log.Information("Token refreshed successfully.");
        return 0;
    }
}
