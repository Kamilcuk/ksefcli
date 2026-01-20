using System.Text.Json;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Authorization;
using Spectre.Console.Cli;

namespace KSeFCli;

public class TokenRefreshCommand : AsyncCommand<TokenRefreshCommand.Settings>
{
    private readonly IKSeFClient _ksefClient;

    public TokenRefreshCommand(IKSeFClient ksefClient)
    {
        _ksefClient = ksefClient;
    }

    public class Settings : GlobalSettings
    {
        [CommandOption("--refresh-token")]
        public string RefreshToken { get; set; } = null!;
    }
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken = default)
    {
        RefreshTokenResponse refreshedAccessTokenResponse = await _ksefClient.RefreshAccessTokenAsync(settings.RefreshToken).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(refreshedAccessTokenResponse));
        return 0;
    }
}
