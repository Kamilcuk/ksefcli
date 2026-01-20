using System.ComponentModel;
using System.Text.Json;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Invoices;
using Spectre.Console.Cli;

namespace KSeFCli;

[Description("Query invoice metadata")]
public class SzukajFakturCommand : AsyncCommand<SzukajFakturCommand.QueryMetadataSettings>
{
    private readonly IKSeFClient _ksefClient;

    public SzukajFakturCommand(IKSeFClient ksefClient)
    {
        _ksefClient = ksefClient;
    }

    public class QueryMetadataSettings : GlobalSettings
    {
        [CommandOption("-s|--subject-type")]
        [Description(@"
                Enum: ""Subject1"" ""Subject2"" ""Subject3"" ""SubjectAuthorized""

                Typ podmiotu, którego dotyczą kryteria filtrowania metadanych faktur. Określa kontekst, w jakim przeszukiwane są dane.
                Wartość \tOpis
                Subject1 \tPodmiot 1 - sprzedawca
                Subject2 \tPodmiot 2 - nabywca
                Subject3 \tPodmiot 3
                SubjectAuthorized \tPodmiot upoważniony
            ")]
        public string SubjectType { get; set; } = null!;

        [CommandOption("--from")]
        [Description("Data początkowa zakresu w formacie ISO-8601 np. 2026-01-03T13:45:00+00:00.")]
        public DateTime From { get; set; }

        [CommandOption("--to")]
        [Description("Data końcowowa zakresu w formacie ISO-8601 np. 2026-01-03T13:45:00+00:00.")]
        public DateTime To { get; set; }

        [CommandOption("--date-type")]
        [Description("Typ daty, według której ma być zastosowany zakres.\n" +
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
    public override async Task<int> ExecuteAsync(CommandContext context, QueryMetadataSettings settings, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse(settings.SubjectType, true, out InvoiceSubjectType subjectType))
        {
            Console.Error.WriteLine($"Invalid SubjectType: {settings.SubjectType}");
            return 1;
        }

        if (!Enum.TryParse(settings.DateType, true, out DateType dateType))
        {
            Console.Error.WriteLine($"Invalid DateType: {settings.DateType}");
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

        PagedInvoiceResponse pagedInvoicesResponse = await _ksefClient.QueryInvoiceMetadataAsync(
            invoiceQueryFilters,
            settings.Token,
            pageOffset: settings.PageOffset,
            pageSize: settings.PageSize,
            cancellationToken: CancellationToken.None).ConfigureAwait(false);

        Console.WriteLine(JsonSerializer.Serialize(pagedInvoicesResponse));
        return 0;
    }
}
