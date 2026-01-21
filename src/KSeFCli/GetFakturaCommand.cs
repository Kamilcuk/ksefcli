using CommandLine;
using KSeF.Client.Clients;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Threading.Tasks;

namespace KSeFCli;

[Verb("GetFaktura", HelpText = "Get a single invoice by KSeF number")]
public class GetFakturaCommand : GlobalCommand
{
    [Option('k', "ksef-number", Required = true, HelpText = "KSeF invoice number")]
    public string KsefNumber { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceProvider = GetServiceProvider();
        var ksefClient = serviceProvider.GetRequiredService<KSeFClient>();


        string invoice = await ksefClient.GetInvoiceAsync(KsefNumber, Token, cancellationToken).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(new { Invoice = invoice }));
        return 0;
    }
}
