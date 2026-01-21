using System.Text.Json;
using KSeF.Client.ClientFactory;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Authorization;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace KSeFCli;

public class TokenRefreshCommand : BaseKsefCommand<TokenRefreshCommand.Settings>
{
    private readonly ILogger<TokenRefreshCommand> _logger;

    public TokenRefreshCommand(ILogger<TokenRefreshCommand> logger, IKSeFClientFactory ksefClientFactory)
        : base(ksefClientFactory)
    {
        _logger = logger;
    }

    public class Settings : GlobalSettings
    {
        [CommandOption("--refresh-token")]
        public string RefreshToken { get; set; } = null!;
    }
    public override async Task<int> ExecuteWithProfileAsync(CommandContext context, Settings settings, ProfileConfig profile, IKSeFClient client, CancellationToken cancellationToken)
    {
        if (profile.AuthMethod != AuthMethod.KsefToken)
        {
            _logger.LogError("Token refresh requires KSeF Token authentication.");
            return 1;
        }

        RefreshTokenResponse refreshedAccessTokenResponse = await client.RefreshAccessTokenAsync(settings.RefreshToken).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(refreshedAccessTokenResponse));
        _logger.LogInformation("Token refreshed successfully.");
        return 0;
    }
}
