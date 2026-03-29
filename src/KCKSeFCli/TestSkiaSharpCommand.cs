using CommandLine;
using KSeF.Client.Api.Services;

namespace KCKSeFCli;

[Verb("TestSkiaSharp", HelpText = "Internal command to test SkiaSharp QR generation.")]
public class TestSkiaSharpCommand : IGlobalCommand {
    [Value(0, Required = true, HelpText = "Output file path.")]
    public required string OutputPath { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        byte[] qrCodeBytes = QrCodeService.GenerateQrCode("https://example.com", 5);
        File.WriteAllBytes(OutputPath, qrCodeBytes);
        Console.WriteLine($"Test QR code saved to {OutputPath}");
        return await Task.FromResult(0).ConfigureAwait(false);
    }
}
