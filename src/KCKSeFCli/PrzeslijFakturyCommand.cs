using System.Text.Json;
using System.Xml.Linq;

using CommandLine;

using KSeF.Client.Api.Builders.Batch;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KCKSeFCli.Utils;

using Microsoft.Extensions.DependencyInjection;



namespace KCKSeFCli;

[Verb("PrzeslijFaktury", HelpText = "Upload invoices in XML format.")]
public class PrzeslijFakturyCommand : IWithConfigCommand {
    [Value(0, Min = 1, Required = true, HelpText = "Paths to XML invoice files.")]
    public required IEnumerable<string> Pliki { get; set; }

    [Option('u', "upodir", Required = false, HelpText = "katalog do zapisu plikow upo")]
    public string? UpoDir { get; set; }

    [Option("upopdf", Required = false, HelpText = "convertuj upo od razu na pdf")]
    public bool UpoPdf { get; set; }

    [Option("uposesji", Required = false, HelpText = "Zapisz UPO sesji (zbiorcze upo)")]
    public bool UpoSesji { get; set; } = false;

    [Option("offlinemode", Required = false, HelpText = "Ustaw jeśli chcesz ustawic offline mode")]
    public bool OfflineModeOption { get; set; } = false;

    public static IEnumerable<(string FileName, byte[] Content)> GetFilesWithContent(IEnumerable<string> paths) {
        return paths.Select(path => (
            FileName: Path.GetFileName(path),
            Content: File.ReadAllBytes(path)
        ));
    }

    private sealed record OpenBatchSessionResult(
        string ReferenceNumber,
        OpenBatchSessionResponse OpenBatchSessionResponse,
        List<BatchPartSendingInfo> EncryptedParts
    );

    private const SystemCode DefaultSystemCode = SystemCode.FA3;
    private const string DefaultSchemaVersion = "1-0E";
    private const string DefaultValue = "FA";

    /// <summary>
    /// Buduje żądanie otwarcia sesji wsadowej z kodem formularza i listą zaszyfrowanych partów.
    /// </summary>
    /// <param name="zipMeta">Metadane pliku ZIP.</param>
    /// <param name="encryption">Dane szyfrowania.</param>
    /// <param name="encryptedParts">Lista zaszyfrowanych partów.</param>
    /// <param name="systemCode">Kod systemowy formularza.</param>
    /// <param name="schemaVersion">Wersja schematu.</param>
    /// <param name="value">Wartość formularza.</param>
    /// <returns>Obiekt żądania otwarcia sesji wsadowej.</returns>
    private static OpenBatchSessionRequest BuildOpenBatchRequest(
        FileMetadata zipMeta,
        EncryptionData encryption,
        IEnumerable<BatchPartSendingInfo> encryptedParts,
        SystemCode systemCode = DefaultSystemCode,
        string schemaVersion = DefaultSchemaVersion,
        string value = DefaultValue,
        bool offlineMode = false) {
        IOpenBatchSessionRequestBuilderBatchFile builder = OpenBatchSessionRequestBuilder
            .Create()
            .WithFormCode(systemCode: SystemCodeHelper.GetSystemCode(systemCode), schemaVersion: schemaVersion, value: value)
            .WithOfflineMode(offlineMode)
            .WithBatchFile(fileSize: zipMeta.FileSize, fileHash: zipMeta.HashSHA);

        foreach (BatchPartSendingInfo p in encryptedParts) {
            builder = builder.AddBatchFilePart(
                ordinalNumber: p.OrdinalNumber,
                fileSize: p.Metadata.FileSize,
                fileHash: p.Metadata.HashSHA);
        }

        return builder
            .EndBatchFile()
            .WithEncryption(
                encryptedSymmetricKey: encryption.EncryptionInfo.EncryptedSymmetricKey,
                initializationVector: encryption.EncryptionInfo.InitializationVector)
            .Build();
    }

    private async Task<OpenBatchSessionResult> PrepareAndOpenBatchSessionAsync(
            IEnumerable<(string FileName, byte[] Content)> invoices,
            IKSeFClient ksefClient,
        ICryptographyService cryptographyService,
        string accessToken) {
        EncryptionData encryptionData = cryptographyService.GetEncryptionData();

        Log.Information("1. Przygotowanie paczki ZIP");
        (byte[] zipBytes, FileMetadata zipMeta) =
            BatchUtils.BuildZip(invoices, cryptographyService);

        Log.Information("2. Podział binarny paczki ZIP na części oraz 3. Zaszyfrowanie części paczki");
        List<BatchPartSendingInfo> encryptedParts =
            BatchUtils.EncryptAndSplit(zipBytes, encryptionData, cryptographyService);

        Log.Information("4. Otwarcie sesji wsadowej");
        OpenBatchSessionRequest openBatchRequest = BuildOpenBatchRequest(zipMeta, encryptionData, encryptedParts,
         DefaultSystemCode,
         DefaultSchemaVersion,
         DefaultValue,
         OfflineModeOption);

        OpenBatchSessionResponse openBatchSessionResponse =
            await BatchUtils.OpenBatchAsync(ksefClient, openBatchRequest, accessToken).ConfigureAwait(false);

        return new OpenBatchSessionResult(
            openBatchSessionResponse.ReferenceNumber,
            openBatchSessionResponse,
            encryptedParts
        );
    }

    private static async Task PobranieInformacjiNaTematPrzeslanychFaktur(
            IKSeFClient ksefClient,
            string referenceNumber,
            string accessToken,
            CancellationToken cancellationToken) {
        const int pageSize = 50;
        string? continuationtoken = null;
        do {
            SessionInvoicesResponse sessionInvoices = await ksefClient
                                        .GetSessionInvoicesAsync(
                                        referenceNumber,
                                        accessToken,
                                        pageSize,
                                        continuationtoken,
                                        cancellationToken).ConfigureAwait(false);

            foreach (SessionInvoice sessionInvoice in sessionInvoices.Invoices) {
                Console.Out.WriteLine(JsonSerializer.Serialize(sessionInvoice, new JsonSerializerOptions {
                    WriteIndented = true
                }));
            }

            continuationtoken = sessionInvoices.ContinuationToken;
        }
        while (continuationtoken != null);
    }

    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        XML2PDFCommand.Runner? pdfRunner = null;
        if (UpoPdf) {
            pdfRunner = await XML2PDFCommand.GetRunner(cancellationToken).ConfigureAwait(false);
        }

        IEnumerable<(string FileName, byte[] Content)> invoices = GetFilesWithContent(Pliki);

        string accessToken = await GetAccessToken(scope, cancellationToken).ConfigureAwait(false);
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        ICryptographyService cryptographyService = await GetCryptographicService(scope, cancellationToken).ConfigureAwait(false);

        OpenBatchSessionResult result = await PrepareAndOpenBatchSessionAsync(invoices, ksefClient, cryptographyService, accessToken).ConfigureAwait(false);
        string referenceNumber = result.ReferenceNumber;
        Log.Information($"ReferenceNumber={result.ReferenceNumber}");

        Log.Information("5. Przesłanie zadeklarowanych części paczki");
        await ksefClient.SendBatchPartsAsync(result.OpenBatchSessionResponse, result.EncryptedParts).ConfigureAwait(false);

        Log.Information("6. Zamknięcie sesji wsadowej");
        await ksefClient.CloseBatchSessionAsync(result.ReferenceNumber, accessToken).ConfigureAwait(false);

        /* ---------------------------------------------------------------------- */
        Log.Information("sesja-sprawdzenie-stanu-i-pobranie-upo.md");

        Log.Information("4) Oczekiwanie na przetworzenie faktury");
        SessionStatusResponse sessionStatus = await AsyncPollingUtils.PollWithBackoffAsync(
            action: () => ksefClient.GetSessionStatusAsync(referenceNumber, accessToken, cancellationToken),
            result => result is not null && result.SuccessfulInvoiceCount is not null,
            initialDelay: TimeSpan.FromSeconds(1),
            maxDelay: TimeSpan.FromSeconds(5),
            maxAttempts: 30,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        Log.Information("3. Pobranie informacji na temat przesłanych faktur");
        await PobranieInformacjiNaTematPrzeslanychFaktur(ksefClient, referenceNumber, accessToken, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(UpoDir)) {
            Directory.CreateDirectory(UpoDir);

            if (UpoSesji && sessionStatus.Upo is not null) {
                // Zbiorcze UPO
                foreach (UpoPageResponse? upo in sessionStatus.Upo.Pages) {
                    Log.Information($"Pobieranie zbiorczego UPO: {upo.ReferenceNumber}");
                    string upoContent = await ksefClient.GetSessionUpoAsync(referenceNumber, upo.ReferenceNumber, accessToken, cancellationToken).ConfigureAwait(false);
                    string upoPath = Path.Combine(UpoDir, $"uposesji-{upo.ReferenceNumber}.xml");
                    File.WriteAllText(upoPath, XDocument.Parse(upoContent).ToString() + "\n");
                    if (UpoPdf) {
                        Log.Information($"Generowanie PDF dla zbiorczego UPO: {upo.ReferenceNumber}");
                        byte[] pdfContent = await pdfRunner!.XML2PDF(upoContent, Quiet, true, null, null, cancellationToken).ConfigureAwait(false);
                        File.WriteAllBytes(Path.ChangeExtension(upoPath, ".pdf"), pdfContent);
                    }
                }
            }

            // Indywidualne UPO
            const int pageSize = 50;
            string? continuationtoken = null;
            do {
                SessionInvoicesResponse sessionInvoices = await ksefClient
                   .GetSessionInvoicesAsync(
                       referenceNumber,
                       accessToken,
                       pageSize,
                       continuationtoken,
                       cancellationToken).ConfigureAwait(false);

                foreach (SessionInvoice? invoice in sessionInvoices.Invoices.Where(i => i.KsefNumber is not null)) {
                    Log.Information($"Pobieranie indywidualnego UPO dla faktury: {invoice.KsefNumber}");
                    string upoContent = await ksefClient.GetSessionInvoiceUpoByKsefNumberAsync(referenceNumber, invoice.KsefNumber, accessToken, cancellationToken).ConfigureAwait(false);
                    string upoPath = Path.Combine(UpoDir, $"upo-{invoice.KsefNumber}.xml");
                    File.WriteAllText(upoPath, XDocument.Parse(upoContent).ToString() + "\n");
                    if (UpoPdf) {
                        Log.Information($"Generowanie PDF dla indywidualnego UPO: {invoice.KsefNumber}");
                        byte[] pdfContent = await pdfRunner!.XML2PDF(upoContent, Quiet, true, null, null, cancellationToken).ConfigureAwait(false);
                        File.WriteAllBytes(Path.ChangeExtension(upoPath, ".pdf"), pdfContent);
                    }
                }

                continuationtoken = sessionInvoices.ContinuationToken;
            } while (continuationtoken != null);
        }

        return 0;
    }
}
