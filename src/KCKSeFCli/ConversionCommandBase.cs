using System.IO;

using CommandLine;

namespace KCKSeFCli;

public abstract class ConversionCommandBase : IGlobalCommand {
    [Value(0, Required = true, HelpText = "Input XML file(s), optionally followed by output file or directory.")]
    public IEnumerable<string> Args { get; set; } = [];

    protected (List<string>? InputFiles, string? OutputFile, string? OutputDir) ParseArgs() {
        var args = Args.ToList();
        if (args.Count < 1) {
            Log.Error("Error: No input files specified.");
            return (null, null, null);
        }

        var inputFiles = args.Take(args.Count - 1).ToList();
        var outputArg = args.Count > 1 ? args[^1] : null;

        if (!inputFiles.Any()) {
            Log.Error("Error: No input files specified.");
            return (null, null, null);
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
                    return (null, null, null);
                }
                outputDir = outputArg;
            }
        }

        return (inputFiles, outputFile, outputDir);
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
}