using System.ComponentModel;
using System.Text.Json;
using KSeF.Client.Core.Interfaces.Clients;
using Spectre.Console.Cli;

namespace KSeFCli;

[Description("Get a single invoice by KSeF number")]
public class GetFakturaCommand : AsyncCommand<GetFakturaCommand.Settings>
{
    private readonly IKSeFClient _ksefClient;

    public GetFakturaCommand(IKSeFClient ksefClient)
    {
        _ksefClient = ksefClient;
    }

    public class Settings : GlobalSettings
    {
        [CommandOption("-k|--ksef-number")]
        [Description("KSeF invoice number")]
        public string KsefNumber { get; set; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken = default)
    {
        string invoice = await _ksefClient.GetInvoiceAsync(settings.KsefNumber, settings.Token, CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { Invoice = invoice }));
        return 0;
    }
}
