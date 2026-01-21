using CommandLine;
using System.IO.Compression;
using KSeF.Client.Core.Interfaces;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.Batch;
using Microsoft.Extensions.DependencyInjection;
using KSeF.Client.Core.Models.Builders.Sessions.Batch;
using FileMetadata = KSeF.Client.Core.Models.FileMetadata;

namespace KSeFCli;

[Verb("PrzeslijFaktury", HelpText = "Upload invoices in XML format.")]
public class PrzeslijFakturyCommand : GlobalCommand
{
    [Option('f', "files", Required = true, HelpText = "Paths to XML invoice files.")]
    public IEnumerable<string> Pliki { get; set; }

    [Option("tmpdir", Default = "/tmp", HelpText = "Temporary directory for ZIP file.")]
    public string TempDir { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        string zipPath = Path.Combine(TempDir, $"ksef-invoices-{Path.GetRandomFileName()}.zip");

        try
        {
            if (!Pliki.Any())
            {
                Console.Error.WriteLine("No files to process.");
                return 1;
            }

            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    foreach (string file in Pliki)
                    {
                        if (File.Exists(file))
                        {
                            archive.CreateEntryFromFile(file, Path.GetFileName(file));
                        }
                        else
                        {
                            Console.Error.WriteLine($"File not found: {file}");
                        }
                    }
                }
            }

            Console.WriteLine($"Created ZIP file: {zipPath}");

            using IServiceScope scope = GetScope();
            IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
            ICryptographyService cryptographyService = scope.ServiceProvider.GetRequiredService<ICryptographyService>();
            
            EncryptionData encryptionData = cryptographyService.GetEncryptionData();
            
            byte[] zipBytes = await File.ReadAllBytesAsync(zipPath, cancellationToken);
            FileMetadata zipMetadata = cryptographyService.GetMetaData(zipBytes);
            
            byte[] encryptedZip = cryptographyService.EncryptBytesWithAES256(zipBytes, encryptionData.CipherKey, encryptionData.CipherIv);
            FileMetadata encryptedZipMetadata = cryptographyService.GetMetaData(encryptedZip);

            IOpenBatchSessionRequestBuilderBatchFile builder = OpenBatchSessionRequestBuilder
                .Create()
                .WithFormCode(systemCode: "FA", schemaVersion: "3", value: "FA")
                .WithBatchFile(fileSize: zipMetadata.FileSize, fileHash: zipMetadata.HashSHA);
            
            builder = builder.AddBatchFilePart(
                ordinalNumber: 1,
                fileName: $"part_1.zip.aes",
                fileSize: encryptedZipMetadata.FileSize,
                fileHash: encryptedZipMetadata.HashSHA);
            
            OpenBatchSessionRequest openBatchRequest = builder
                .EndBatchFile()
                .WithEncryption(
                    encryptedSymmetricKey: encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                    initializationVector: encryptionData.EncryptionInfo.InitializationVector)
                .Build();

            OpenBatchSessionResponse openBatchSessionResponse = await ksefClient.OpenBatchSessionAsync(openBatchRequest, await GetAccessToken(cancellationToken), cancellationToken);
            
            Console.WriteLine($"Session opened with reference number: {openBatchSessionResponse.ReferenceNumber}");

            List<BatchPartSendingInfo> encryptedParts = new List<BatchPartSendingInfo>
            {
                new BatchPartSendingInfo(encryptedZip, encryptedZipMetadata, 1)
            };

            await ksefClient.SendBatchPartsAsync(openBatchSessionResponse, encryptedParts, cancellationToken);
            
            Console.WriteLine("File part uploaded successfully.");

            await ksefClient.CloseBatchSessionAsync(openBatchSessionResponse.ReferenceNumber, await GetAccessToken(cancellationToken), cancellationToken);
            
            Console.WriteLine("Session closed successfully.");

            SessionStatusResponse sessionStatus;
            int pollingAttempts = 0;
            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.Error.WriteLine("Operation cancelled.");
                    return 1;
                }

                pollingAttempts++;
                Console.WriteLine($"Checking session status... (Attempt {pollingAttempts})");
                sessionStatus = await ksefClient.GetSessionStatusAsync(openBatchSessionResponse.ReferenceNumber, await GetAccessToken(cancellationToken), cancellationToken);
                Console.WriteLine($"Session status: {sessionStatus.Status}");

                if (sessionStatus.Status == "InProgress")
                {
                    await Task.Delay(5000, cancellationToken); // Wait 5 seconds
                }

            } while (sessionStatus.Status == "InProgress" && pollingAttempts < 20);

            if (sessionStatus.Status == "InProgress")
            {
                Console.Error.WriteLine("Timeout exceeded while waiting for session to complete.");
                return 1;
            }

            Console.WriteLine("Session processing completed.");
            Console.WriteLine($"Successful invoices: {sessionStatus.SuccessfulInvoiceCount}");
            Console.WriteLine($"Failed invoices: {sessionStatus.FailedInvoiceCount}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        finally
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
                Console.WriteLine($"Deleted temporary ZIP file: {zipPath}");
            }
        }
        
        await Task.Yield();
        return 0;
    }
}
