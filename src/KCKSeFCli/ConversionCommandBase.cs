using System.IO;
using System.Text;

using CommandLine;

namespace KCKSeFCli;

public abstract class ConversionCommandBase : IGlobalCommand {
    [Value(0, Required = true, HelpText = "Input XML file(s), optionally followed by output file or directory.")]
    public IEnumerable<string> Args { get; set; } = [];

    [Option("force", Required = false, HelpText = "Force conversion even if output file exists and is newer than input.")]
    public bool Force { get; set; }

    protected record ParsedArgs(List<string> InputFiles, string? OutputFile, string? OutputDir);

    protected ParsedArgs? ParseArgs() {
        var args = Args.ToList();
        if (args.Count < 1) {
            Log.Error("Error: No input files specified.");
            return null;
        }

        var inputFiles = args.Take(args.Count - 1).ToList();
        var outputArg = args.Count > 1 ? args[^1] : null;

        if (!inputFiles.Any()) {
            Log.Error("Error: No input files specified.");
            return null;
        }

        string? outputFile = null;
        string? outputDir = null;

        if (outputArg != null) {
            if (args.Count == 2) {
                if (Directory.Exists(outputArg)) {
                    outputDir = outputArg;
                } else {
                    outputFile = outputArg;
                }
            } else {
                if (!Directory.Exists(outputArg)) {
                    Log.Error($"Error: With multiple inputs, last argument must be an existing directory: {outputArg}");
                    return null;
                }
                outputDir = outputArg;
            }
        }

        return new ParsedArgs(inputFiles, outputFile, outputDir);
    }

    protected string? GetOutputPath(string inputFile, string? outputFile, string? outputDir, string extension, bool allowStdout = false) {
        if (outputFile == "-") {
            if (!allowStdout) {
                Log.Error($"Error: Stdout output not supported for this command.");
                return null;
            }
            return "-";
        }
        if (!string.IsNullOrEmpty(outputFile)) {
            return outputFile;
        }
        if (!string.IsNullOrEmpty(outputDir)) {
            Directory.CreateDirectory(outputDir);
            string fileName = Path.GetFileNameWithoutExtension(inputFile) + extension;
            return Path.Combine(outputDir, fileName);
        }
        return Path.ChangeExtension(inputFile, extension)!;
    }

    protected bool ValidateInputFile(string inputFile) {
        if (!File.Exists(inputFile)) {
            Log.Error($"Error: Input file not found: {inputFile}");
            return false;
        }
        return true;
    }

    protected bool CheckOutputExists(string outputPath) {
        if (outputPath != "-" && File.Exists(outputPath)) {
            Log.Error($"Error: Output file already exists: {outputPath}");
            return true;
        }
        return false;
    }

    protected async Task<int> ProcessListOfFiles<TArgs>(
        ParsedArgs parsedArgs,
        string extension,
        TArgs converterArgs,
        Func<string, TArgs, CancellationToken, Task<byte[]>> converter,
        CancellationToken cancellationToken,
        bool allowStdout = false
    ) {
        var inputFiles = parsedArgs.InputFiles;
        if (!inputFiles.Any()) {
            Log.Error("Error: No input files specified.");
            return 1;
        }

        var outputPaths = new List<string?>();
        foreach (var inputFile in inputFiles) {
            if (!ValidateInputFile(inputFile)) return 1;

            string? outputPath = GetOutputPath(inputFile, parsedArgs.OutputFile, parsedArgs.OutputDir, extension, allowStdout);
            if (outputPath == null) return 1;

            outputPaths.Add(outputPath);
        }

        if (!Force) {
            foreach (var (inputFile, outputPath) in inputFiles.Zip(outputPaths)) {
                if (outputPath != "-" && File.Exists(outputPath)) {
                    var inputTime = File.GetLastWriteTimeUtc(inputFile);
                    var outputTime = File.GetLastWriteTimeUtc(outputPath);
                    if (outputTime >= inputTime) {
                        if (!Quiet) {
                            Log.Information($"Skipping {inputFile} -> {outputPath} (output is up to date)");
                        }
                        continue;
                    }
                }
            }
        }

        for (int i = 0; i < inputFiles.Count; i++) {
            var inputFile = inputFiles[i];
            var outputPath = outputPaths[i];

            if (outputPath == "-") {
                byte[] content = await converter(inputFile, converterArgs, cancellationToken).ConfigureAwait(false);
                Console.WriteLine(Encoding.UTF8.GetString(content));
                continue;
            }

            if (!Force && File.Exists(outputPath)) {
                var inputTime = File.GetLastWriteTimeUtc(inputFile);
                var outputTime = File.GetLastWriteTimeUtc(outputPath);
                if (outputTime >= inputTime) {
                    if (!Quiet) {
                        Log.Information($"Skipping {inputFile} -> {outputPath} (output is up to date)");
                    }
                    continue;
                }
            }

            try {
                byte[] content = await converter(inputFile, converterArgs, cancellationToken).ConfigureAwait(false);
                if (parsedArgs.OutputDir != null) {
                    Directory.CreateDirectory(parsedArgs.OutputDir);
                }
                if (outputPath != null) {
                    File.WriteAllBytes(outputPath, content);
                    if (!Quiet) {
                        Log.Information($"Saved to: {outputPath}");
                    }
                }
            } catch (Exception ex) {
                Log.Error($"Error converting {inputFile}: {ex.Message}");
                return 1;
            }
        }

        return 0;
    }
}