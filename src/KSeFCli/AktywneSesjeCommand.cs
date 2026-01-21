using KSeF.Client.ClientFactory;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using System.Text.Json;

namespace KSeFCli;

public class AktywneSesjeCommand : BaseKsefCommand<AktywneSesjeCommand.Settings>
{
    private readonly ILogger<AktywneSesjeCommand> _logger;

    public AktywneSesjeCommand(ILogger<AktywneSesjeCommand> logger, IKSeFClientFactory ksefClientFactory)
        : base(ksefClientFactory)
    {
        _logger = logger;
    }

    public class Settings : GlobalSettings
    {
    }

    public override async Task<int> ExecuteWithProfileAsync(CommandContext context, Settings settings, ProfileConfig profile, IKSeFClient client, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pobieranie aktywnych sesji...");

        var activeSessions = await client.GetActiveSessions(profile.Token, null, null, cancellationToken).ConfigureAwait(false);

        Console.WriteLine(JsonSerializer.Serialize(activeSessions));

        _logger.LogInformation("Zakończono pomyślnie.");

        return 0;
    }
}
