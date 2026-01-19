using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Authorization;
using Spectre.Console.Cli;

namespace KSeFCli;

public class TokenRefreshCommand : AsyncCommand<TokenRefreshCommand.Settings> {
    public class Settings : GlobalSettings {
        [CommandOption("--refresh-token")]
        public string RefreshToken { get; set; } = null!;
    }
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings) {
        IKSeFClient ksefClient = KSeFClientFactory.CreateKSeFClient(settings);
        RefreshTokenResponse refreshedAccessTokenResponse = await ksefClient.RefreshAccessTokenAsync(settings.RefreshToken).ConfigureAwait(false);
        return 0;
    }
}
