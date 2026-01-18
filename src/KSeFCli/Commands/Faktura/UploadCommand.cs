using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using KSeFCli.Services;

namespace KSeFCli.Commands.Faktura
{
    public sealed class UploadCommand : AsyncCommand<UploadCommand.Settings>
    {
        private readonly InvoiceService _invoiceService;

        public UploadCommand(InvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        public sealed class Settings : CommandSettings
        {
            [CommandArgument(0, "<FILES>")]
            [Description("XML invoice files to upload.")]
            public string[] Files { get; set; } = null!;

            public override ValidationResult Validate()
            {
                if (Files == null || Files.Length == 0)
                {
                    return ValidationResult.Error("At least one file must be specified.");
                }
                return ValidationResult.Success();
            }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            var exitCode = 0;
            foreach (var filePath in settings.Files)
            {
                var referenceNumber = await _invoiceService.UploadInvoiceAsync(filePath, cancellationToken);
                if (referenceNumber == null)
                {
                    exitCode = 1; // Indicate failure
                }
            }
            return exitCode;
        }
    }
}
