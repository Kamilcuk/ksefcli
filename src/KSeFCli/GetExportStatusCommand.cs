using System.ComponentModel;
using System.Text.Json;
using KSeF.Client.ClientFactory;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Invoices;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace KSeFCli;


[Description("Checks the status of an asynchronous export operation")]
public class GetExportStatusCommand : BaseKsefCommand<GetExportStatusCommand.GetExportStatusSettings>
{
    private readonly ILogger<GetExportStatusCommand> _logger;

    public GetExportStatusCommand(ILogger<GetExportStatusCommand> logger, IKSeFClientFactory ksefClientFactory)
        : base(ksefClientFactory)
    {
        _logger = logger;
    }

    public class GetExportStatusSettings : GlobalSettings
    {
        [CommandOption("-r|--reference-number")]
        [Description("Reference number of the asynchronous export operation")]
        public string ReferenceNumber { get; set; } = null!;
    }
    public override async Task<int> ExecuteWithProfileAsync(CommandContext context, GetExportStatusSettings settings, ProfileConfig profile, IKSeFClient client, CancellationToken cancellationToken)
    {
        if (profile.AuthMethod != AuthMethod.KsefToken)
        {
            _logger.LogError("Checking export status requires KSeF Token authentication.");
            return 1;
        }

        InvoiceExportStatusResponse exportStatus = await client.GetInvoiceExportStatusAsync(
            settings.ReferenceNumber,
            profile.Token!).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(exportStatus));
        _logger.LogInformation("Export status retrieved successfully.");
        return 0;
    }
}
