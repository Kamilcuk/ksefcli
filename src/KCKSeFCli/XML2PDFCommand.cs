using System.Diagnostics;

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

        Runner runner = await GetRunner(cancellationToken).ConfigureAwait(false);
        byte[] pdfContent = await runner.XML2PDF(xmlContent, Quiet, Upo, NrKSeF, QrCode, cancellationToken).ConfigureAwait(false);

        await File.WriteAllBytesAsync(outputPdfPath, pdfContent, cancellationToken).ConfigureAwait(false);

        Console.WriteLine($"PDF saved to: {outputPdfPath}");

        return 0;
    }

    public class Runner
    {
        private readonly string[] _command;

        internal Runner(string[] command)
        {
            _command = command;
        }

        public async Task<byte[]> XML2PDF(string xmlContent, bool quiet, bool upo, string? nrKSeF, string? qrCode, CancellationToken cancellationToken)
        {
            using TemporaryFile tempXml = new TemporaryFile(extension: ".xml");
            await File.WriteAllTextAsync(tempXml.Path, xmlContent, cancellationToken).ConfigureAwait(false);
            using TemporaryFile tempPdf = new TemporaryFile(extension: ".pdf");

            List<string> commandArgs = new(_command);
            commandArgs.AddRange(new[] { upo ? "upo" : "invoice", tempXml.Path, tempPdf.Path });

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
    }

    private static void AssertNpxExists()
    {
        if (!Subprocess.CheckCommandExists("npx"))
        {
            throw new InvalidOperationException("Command `npx` not found. Please install Node.js and npm to use this functionality.");
        }
    }

    public static async Task<Runner> GetRunner(CancellationToken cancellationToken)
    {
        string? url = null;
        string? fileName = null;

        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
        {
            url = "https://github.com/Kamilcuk/ksef-pdf-generator/releases/download/1.0.0/ksef-pdf-generator-linux";
            fileName = "ksef-pdf-generator-linux";
        }
        else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            url = "https://github.com/Kamilcuk/ksef-pdf-generator/releases/download/1.0.0/ksef-pdf-generator-win.exe";
            fileName = "ksef-pdf-generator-win.exe";
        }

        string[] runnerCommand;

        if (url is null || fileName is null)
        {
            AssertNpxExists();
            runnerCommand = new[] { "npx", "--yes", "github:kamilcuk/ksef-pdf-generator" };
        }
        else
        {
            Directory.CreateDirectory(IGlobalCommand.CacheDir);
            string destinationPath = Path.Combine(IGlobalCommand.CacheDir, fileName);

            await Downloader.DownloadFileWithTimestampCheckAsync(url, destinationPath, cancellationToken).ConfigureAwait(false);

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                Process p = new System.Diagnostics.Process
                {
                    StartInfo = { FileName = "chmod", Arguments = $"+x \"{destinationPath}\"" }
                };
                p.Start();
                await p.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            runnerCommand = new[] { destinationPath };
        }

        return new Runner(runnerCommand);
    }
}
