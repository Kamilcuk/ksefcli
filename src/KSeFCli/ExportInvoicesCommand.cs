using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using KSeF.Client.ClientFactory;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace KSeFCli;

[Description("Initialize an asynchronous invoice export")]
public class ExportInvoicesCommand : BaseKsefCommand<ExportInvoicesCommand.ExportInvoicesSettings>
{
    private readonly IKSeFFactoryCryptographyServices _cryptographyServiceFactory;
    private readonly ILogger<ExportInvoicesCommand> _logger;

    public ExportInvoicesCommand(IKSeFFactoryCryptographyServices cryptographyServiceFactory, ILogger<ExportInvoicesCommand> logger, IKSeFClientFactory ksefClientFactory)
        : base(ksefClientFactory)
    {
        _cryptographyServiceFactory = cryptographyServiceFactory;
        _logger = logger;
    }

    public class ExportInvoicesSettings : GlobalSettings
    {
        [CommandOption("--from")]
        [Description("Data początkowa zakresu w formacie ISO-8601 np. 2026-01-03T13:45:00+00:00.")]
        public DateTime From { get; set; }

        [CommandOption("--to")]
        [Description("Data końcowa zakresu w formacie ISO-8601 np. 2026-01-03T13:45:00+00:00.")]
        public DateTime To { get; set; }

        [CommandOption("--date-type")]
        [Description(@"Typ daty, według której ma być zastosowany zakres.\n" +
                     "Dostępne wartości:\n" +
                     "  \"Issue\" - Data wystawienia faktury.\n" +
                     "  \"Invoicing\" - Data przyjęcia faktury w systemie KSeF (do dalszego przetwarzania).\n" +
                     "  \"PermanentStorage\" - Data trwałego zapisu faktury w repozytorium systemu KSeF.")]
        [DefaultValue("Issue")]
        public string DateType { get; set; } = "Issue";

        [CommandOption("-s|--subject-type")]
        [Description("Invoice subject type (e.g., Subject1, Subject2, Subject3)")]
        public string SubjectType { get; set; } = null!;
    }

    public override async Task<int> ExecuteWithProfileAsync(CommandContext context, ExportInvoicesSettings settings, ProfileConfig profile, IKSeFClient client, CancellationToken cancellationToken)
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

        if (profile.AuthMethod != AuthMethod.Xades)
        {
            _logger.LogError("Exporting invoices requires XAdES authentication.");
            return 1;
        }

        KSeF.Client.ClientFactory.Environment environment = Enum.Parse<KSeF.Client.ClientFactory.Environment>(profile.Environment, true);
        ICryptographyService cryptographyService = await _cryptographyServiceFactory.CryprographyService(environment).ConfigureAwait(false);

        string certificatePath = profile.Certificate!.Certificate;
        string privateKeyPath = profile.Certificate.Private_Key;
        string certificatePassword = System.Environment.GetEnvironmentVariable(profile.Certificate.Password_Env)!;

        X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(privateKeyPath, certificatePassword);

        EncryptionData encryptionData = cryptographyService.GetEncryptionData();

        InvoiceQueryFilters queryFilters = new InvoiceQueryFilters
        {
            DateRange = new DateRange
            {
                From = settings.From,
                To = settings.To,
                DateType = dateType
            },
            SubjectType = subjectType
        };

        InvoiceExportRequest invoiceExportRequest = new InvoiceExportRequest
        {
            Encryption = encryptionData.EncryptionInfo,
            Filters = queryFilters
        };

        OperationResponse exportInvoicesResponse = await client.ExportInvoicesAsync(
            invoiceExportRequest,
            null, // No token needed for XAdES export, it's certificate based
            CancellationToken.None).ConfigureAwait(false);

        Console.WriteLine(JsonSerializer.Serialize(new { ReferenceNumber = exportInvoicesResponse.ReferenceNumber }));
        _logger.LogInformation("Export invoices operation initiated successfully.");
        return 0;
    }
}
