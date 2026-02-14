using CommandLine;

namespace KCKSeFCli;

[Verb("XML2PDF", HelpText = "Convert KSeF XML invoice to PDF.")]
public class XML2PDFCommand : IGlobalCommand
{
    [Value(0, Required = true, HelpText = "Input XML file path.")]
    public required string InputFile { get; set; }

    [Value(1, HelpText = "Output PDF file path.")]
    public string? OutputFile { get; set; }

    [Option("upo", Required = false, HelpText = "use UPO template")]
    public bool Upo { get; set; }

    [Option("nrKSeF", Required = false, HelpText = "KSeF invoice number to embed in PDF.")]
    public string? NrKSeF { get; set; }

    [Option("qrCode", Required = false, HelpText = "QR code to embed in PDF.")]
    public string? QrCode { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        ConfigureLogging();

        if (!File.Exists(InputFile))
        {
            Console.Error.WriteLine($"Error: Input file not found: {InputFile}");
            return 1;
        }

        string outputPdfPath;
        if (string.IsNullOrEmpty(OutputFile))
        {
            if (!InputFile.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Error: Input file must have a .xml extension when no output file is specified.");
                return 1;
            }
            outputPdfPath = Path.ChangeExtension(InputFile, ".pdf");
            if (File.Exists(outputPdfPath))
            {
                Console.Error.WriteLine($"Error: Output file already exists: {outputPdfPath}");
                return 1;
            }
        }
        else
        {
            outputPdfPath = OutputFile;
        }

        string xmlContent = await File.ReadAllTextAsync(InputFile, cancellationToken).ConfigureAwait(false);
        byte[] pdfContent = await XML2PDF(xmlContent, Quiet, Upo, NrKSeF, QrCode, cancellationToken).ConfigureAwait(false);

        await File.WriteAllBytesAsync(outputPdfPath, pdfContent, cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"PDF saved to: {outputPdfPath}");

        return 0;
    }

    public static async Task<byte[]> XML2PDF(string xmlContent, bool quiet, bool upo, string? nrKSeF, string? qrCode, CancellationToken cancellationToken)
    {
        AssertNpxExists();
        using TemporaryFile tempXml = new TemporaryFile(extension: ".xml");
        await File.WriteAllTextAsync(tempXml.Path, xmlContent, cancellationToken).ConfigureAwait(false);
        using TemporaryFile tempPdf = new TemporaryFile(extension: ".pdf");
        string scriptPath = Path.Combine(AppContext.BaseDirectory, "run-pdf-generator.mjs");
        List<string> commandArgs = new() { "npx", "--yes", "github:kamilcuk/ksef-pdf-generator", upo ? "upo" : "invoice", tempXml.Path, tempPdf.Path };

        System.Collections.Generic.Dictionary<string, string> options = new();
        if (!string.IsNullOrEmpty(nrKSeF))
        {
            options.Add("nrKSeF", nrKSeF);
        }
        if (!string.IsNullOrEmpty(qrCode))
        {
            options.Add("qrCode", qrCode);
        }

        if (options.Count > 0)
        {
            commandArgs.Add(System.Text.Json.JsonSerializer.Serialize(options));
        }

        Subprocess nodeScript = new(
            CommandAndArgs: commandArgs.ToArray(),
            Quiet: quiet
        );
        await nodeScript.CheckCallAsync(cancellationToken).ConfigureAwait(false);
        byte[] pdfBytes = await File.ReadAllBytesAsync(tempPdf.Path, cancellationToken).ConfigureAwait(false);
        return pdfBytes;
    }

    public static void AssertNpxExists()
    {
        if (!Subprocess.CheckCommandExists("npx"))
        {
            throw new InvalidOperationException("Command `npx` not found. Please install Node.js and npm to use this functionality.");
        }
    }
}
