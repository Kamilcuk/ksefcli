using System.ComponentModel;
using System.Text.Json;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Invoices;
using Spectre.Console.Cli;

namespace KSeFCli;


[Description("Checks the status of an asynchronous export operation")]
public class GetExportStatusCommand : AsyncCommand<GetExportStatusCommand.GetExportStatusSettings>
{
    private readonly IKSeFClient _ksefClient;

    public GetExportStatusCommand(IKSeFClient ksefClient)
    {
        _ksefClient = ksefClient;
    }

    public class GetExportStatusSettings : GlobalSettings
    {
        [CommandOption("-r|--reference-number")]
        [Description("Reference number of the asynchronous export operation")]
        public string ReferenceNumber { get; set; } = null!;
    }
    public override async Task<int> ExecuteAsync(CommandContext context, GetExportStatusSettings settings, CancellationToken cancellationToken = default)
    {
        InvoiceExportStatusResponse exportStatus = await _ksefClient.GetInvoiceExportStatusAsync(
            settings.ReferenceNumber,
            settings.Token).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(exportStatus));
        return 0;
    }
}
