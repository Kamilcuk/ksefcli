using CommandLine;
using KSeF.Client.Clients;
using KSeF.Client.Core.Models.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using KSeF.Client.Api.Builders.Certificates;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Extensions;
using KSeF.Client.Core.Models.Certificates;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;


namespace KSeFCli;

[Verb("TokenRefresh", HelpText = "Refresh an existing session token")]
public class TokenRefreshCommand : GlobalCommand
{
    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceProvider = GetServiceProvider();
        IKSeFClient ksefClient = serviceProvider.GetRequiredService<IKSeFClient>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        if (string.IsNullOrEmpty(Token))
        {
            Console.Error.WriteLine("No refresh token provided. Use --token to provide a refresh token.");
            return 1;
        }
        logger.LogInformation("Refreshing token...");
        var tokenResponse = await ksefClient.RefreshAccessTokenAsync(Token, cancellationToken).ConfigureAwait(false);
        Console.Out.WriteLine(JsonSerializer.Serialize(tokenResponse));
        logger.LogInformation("Token refreshed successfully.");
        return 0;
    }
}
