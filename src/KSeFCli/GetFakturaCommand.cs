using System.ComponentModel;
using System.Text.Json;
using KSeF.Client.ClientFactory;
using KSeF.Client.Core.Interfaces.Clients;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace KSeFCli;

[Description("Get a single invoice by KSeF number")]
public class GetFakturaCommand : BaseKsefCommand<GetFakturaCommand.Settings>
{
    private readonly ILogger<GetFakturaCommand> _logger;

    public GetFakturaCommand(ILogger<GetFakturaCommand> logger, IKSeFClientFactory ksefClientFactory)
        : base(ksefClientFactory)
    {
        _logger = logger;
    }

    public class Settings : GlobalSettings
    {
        [CommandOption("-k|--ksef-number")]
        [Description("KSeF invoice number")]
        public string KsefNumber { get; set; } = null!;
    }

    public override async Task<int> ExecuteWithProfileAsync(CommandContext context, Settings settings, ProfileConfig profile, IKSeFClient client, CancellationToken cancellationToken)
    {
        if (profile.AuthMethod != AuthMethod.KsefToken)
        {
            _logger.LogError("Getting invoice by KSeF number requires KSeF Token authentication.");
            return 1;
        }

        string invoice = await client.GetInvoiceAsync(settings.KsefNumber, profile.Token!, CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { Invoice = invoice }));
        _logger.LogInformation("Invoice retrieved successfully.");
        return 0;
    }
}
