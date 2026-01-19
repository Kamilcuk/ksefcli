using System.ComponentModel;
using System.Text.Json;
using KSeF.Client.Core.Interfaces.Clients;
using Spectre.Console.Cli;

namespace KSeFCli;

[Description("Get a single invoice by KSeF number")]
public class GetFakturaCommand : AsyncCommand<GetFakturaCommand.Settings> {
    public class Settings : GlobalSettings {
        [CommandOption("-k|--ksef-number")]
        [Description("KSeF invoice number")]
        public string KsefNumber { get; set; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, GetFakturaCommand.Settings settings) {
        IKSeFClient ksefClient = KSeFClientFactory.CreateKSeFClient(settings);
        string invoice = await ksefClient.GetInvoiceAsync(settings.KsefNumber, settings.Token, CancellationToken.None).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { Status = "Success", Invoice = invoice }));
        return 0;
    }
}
