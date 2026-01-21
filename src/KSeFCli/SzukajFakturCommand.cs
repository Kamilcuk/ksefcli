using System.ComponentModel;
using System.Text.Json;
using KSeF.Client.ClientFactory;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Invoices;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace KSeFCli;

[Description("Query invoice metadata")]
public class SzukajFakturCommand : BaseKsefCommand<SzukajFakturCommand.QueryMetadataSettings>
{
    private readonly ILogger<SzukajFakturCommand> _logger;

    public SzukajFakturCommand(ILogger<SzukajFakturCommand> logger, IKSeFClientFactory ksefClientFactory)
        : base(ksefClientFactory)
    {
        _logger = logger;
    }

    public class QueryMetadataSettings : GlobalSettings
    {
        [CommandOption("-s|--subject-type")]
        [Description("""
                Enum: "Subject1" "Subject2" "Subject3" "SubjectAuthorized"

                Typ podmiotu, którego dotyczą kryteria filtrowania metadanych faktur. Określa kontekst, w jakim przeszukiwane są dane.
                Wartość 	Opis
                Subject1 	Podmiot 1 - sprzedawca
                Subject2 	Podmiot 2 - nabywca
                Subject3 	Podmiot 3
                SubjectAuthorized 	Podmiot upoważniony
            """)]
        public string SubjectType { get; set; } = null!;

        [CommandOption("--from")]
        [Description("Data początkowa zakresu w formacie ISO-8601 np. 2026-01-03T13:45:00+00:00.")]
        public DateTime From { get; set; }

        [CommandOption("--to")]
        [Description("Data końcowawa zakresu w formacie ISO-8601 np. 2026-01-03T13:45:00+00:00.")]
        public DateTime To { get; set; }

        [CommandOption("--date-type")]
        [Description(@"Typ daty, według której ma być zastosowany zakres.\n" +
                     "Dostępne wartości:\n" +
                     "  \"Issue\" - Data wystawienia faktury.\n" +
                     "  \"Invoicing\" - Data przyjęcia faktury w systemie KSeF (do dalszego przetwarzania).\n" +
                     "  \"PermanentStorage\" - Data trwałego zapisu faktury w repozytorium systemu KSeF.")]
        [DefaultValue("Issue")]
        public string DateType { get; set; } = "Issue";

        [CommandOption("--page-offset")]
        [Description("Page offset for pagination")]
        [DefaultValue(0)]
        public int PageOffset { get; set; }

        [CommandOption("--page-size")]
        [Description("Page size for pagination")]
        [DefaultValue(10)]
        public int PageSize { get; set; }
    }
    public override async Task<int> ExecuteWithProfileAsync(CommandContext context, QueryMetadataSettings settings, ProfileConfig profile, IKSeFClient client, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(settings.SubjectType, true, out InvoiceSubjectType subjectType))
        {
            _logger.LogError($"Invalid SubjectType: {settings.SubjectType}");
            return 1;
        }

        if (!Enum.TryParse(settings.DateType, true, out DateType dateType))
        {
            _logger.LogError($"Invalid DateType: {settings.DateType}");
            return 1;
        }

        if (profile.AuthMethod != AuthMethod.KsefToken)
        {
            _logger.LogError("Querying invoice metadata requires KSeF Token authentication.");
            return 1;
        }

        InvoiceQueryFilters invoiceQueryFilters = new InvoiceQueryFilters
        {
            SubjectType = subjectType,
            DateRange = new DateRange
            {
                From = settings.From,
                To = settings.To,
                DateType = dateType
            }
        };

        PagedInvoiceResponse pagedInvoicesResponse = await client.QueryInvoiceMetadataAsync(
            invoiceQueryFilters,
            profile.Token!, // Use token from profile config
            pageOffset: settings.PageOffset,
            pageSize: settings.PageSize,
            cancellationToken: CancellationToken.None).ConfigureAwait(false);

        Console.WriteLine(JsonSerializer.Serialize(pagedInvoicesResponse));
        _logger.LogInformation("Invoice metadata queried successfully.");
        return 0;
    }
}
