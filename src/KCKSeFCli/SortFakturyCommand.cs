using System.Text.Json;
using System.Xml.Linq;

using CommandLine;

namespace KCKSeFCli;

[Verb("SortFaktury", HelpText = "Organize invoice files into structured directories by role/date.")]
public class SortFakturyCommand : IGlobalCommand {
    [Option('o', "output-dir", HelpText = "Output directory (default: current working directory).")]
    public string? OutputDir { get; set; }

    [Option('n', "dryrun", HelpText = "Show what would be done without making changes.")]
    public bool DryRun { get; set; }

    [Option('p', "pdf", HelpText = "Include PDF files in sorting.")]
    public bool IncludePdf { get; set; } = true;

    [Option('j', "json", HelpText = "Include JSON files in sorting.")]
    public bool IncludeJson { get; set; } = true;

    [Option("xml", HelpText = "Include XML files in sorting.")]
    public bool IncludeXml { get; set; } = true;

    [Option("no-move", HelpText = "Copy files instead of moving (default is move).")]
    public bool NoMove { get; set; }

    [Option("nip", HelpText = "Your NIP to determine role (sprzedawca/nabywca). If not provided, defaults to sprzedawca.")]
    public string? Nip { get; set; }

    [Value(0, HelpText = "XML files to sort. If omitted, scans current directory for invoice files.")]
    public IEnumerable<string> InputFiles { get; set; } = [];

    public override Task<int> ExecuteAsync(CancellationToken cancellationToken) {
        ConfigureLogging();

        string outputDir = OutputDir ?? Directory.GetCurrentDirectory();

        var filesToProcess = GetFilesToProcess();
        if (!filesToProcess.Any()) {
            Log.Warning("No invoice files found to process.");
            return Task.FromResult(0);
        }

        if (!DryRun) {
            Directory.CreateDirectory(outputDir);
        }

        var groups = GroupFilesByBaseName(filesToProcess);
        int processed = 0;

        string myNip = Nip?.Replace("-", "").Replace(" ", "") ?? "";

        foreach (var group in groups) {
            var invoiceData = ExtractInvoiceData(group, myNip);
            if (invoiceData == null) {
                Log.Warning($"Could not extract invoice data for {group.Key}, skipping.");
                continue;
            }

            string roleDir = invoiceData.Role;
            string dateDir = invoiceData.DateDir;
            string targetDir = Path.Combine(outputDir, roleDir, dateDir);

            string baseName = $"{invoiceData.KsefNumber} {invoiceData.SellerFirst} {invoiceData.BuyerFirst} {invoiceData.GrossAmount:F2}";
            baseName = SanitizeFileName(baseName);

            foreach (var file in group.Value) {
                string ext = Path.GetExtension(file);
                string targetFile = Path.Combine(targetDir, $"{baseName}{ext}");

                if (File.Exists(targetFile)) {
                    targetFile = GetUniqueFileName(targetDir, baseName, ext);
                }

                bool moveFiles = !NoMove;
                if (DryRun) {
                    Log.Information($"[DRY RUN] {(moveFiles ? "Move" : "Copy")} {file} -> {targetFile}");
                } else {
                    if (!Directory.Exists(targetDir)) {
                        Directory.CreateDirectory(targetDir);
                    }
                    if (moveFiles) {
                        File.Move(file, targetFile);
                        Log.Information($"Moved {file} -> {targetFile}");
                    } else {
                        File.Copy(file, targetFile);
                        Log.Information($"Copied {file} -> {targetFile}");
                    }
                }
                processed++;
            }
        }

        Log.Information($"Processed {processed} files.");
        return Task.FromResult(0);
    }

    internal List<string> GetFilesToProcess() {
        List<string> files = new List<string>();

        if (InputFiles.Any()) {
            foreach (var f in InputFiles) {
                if (File.Exists(f)) {
                    files.Add(Path.GetFullPath(f));
                }
            }
        } else {
            string cwd = Directory.GetCurrentDirectory();
            List<string> extensions = new List<string>();
            if (IncludeXml) extensions.Add(".xml");
            if (IncludeJson) extensions.Add("_summary.json");
            if (IncludePdf) extensions.Add(".pdf");

            foreach (var ext in extensions) {
                files.AddRange(Directory.GetFiles(cwd, $"*{ext}", SearchOption.TopDirectoryOnly));
            }
        }

        return files.Distinct().ToList();
    }

    internal Dictionary<string, List<string>> GroupFilesByBaseName(List<string> files) {
        Dictionary<string, List<string>> groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files) {
            string baseName = GetBaseName(file);
            if (!groups.ContainsKey(baseName)) {
                groups[baseName] = new List<string>();
            }
            groups[baseName].Add(file);
        }

        return groups;
    }

    internal string GetBaseName(string filePath) {
        string fileName = Path.GetFileName(filePath);
        if (fileName.EndsWith("_summary.json", StringComparison.OrdinalIgnoreCase)) {
            return fileName[..^"_summary.json".Length];
        }
        if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) {
            return fileName[..^".xml".Length];
        }
        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) {
            return fileName[..^".pdf".Length];
        }
        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
            return fileName[..^".json".Length];
        }
        return Path.GetFileNameWithoutExtension(fileName);
    }

    private InvoiceData? ExtractInvoiceData(KeyValuePair<string, List<string>> group, string myNip) {
        string? xmlFile = group.Value.FirstOrDefault(f => f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        string? jsonFile = group.Value.FirstOrDefault(f => f.EndsWith("_summary.json", StringComparison.OrdinalIgnoreCase));

        string? sellerName = null;
        string? buyerName = null;
        string? sellerNip = null;
        string? buyerNip = null;
        decimal grossAmount = 0;
        string ksefNumber = group.Key;

        if (!string.IsNullOrEmpty(jsonFile) && File.Exists(jsonFile)) {
            try {
                string jsonContent = File.ReadAllText(jsonFile);
                using JsonDocument doc = JsonDocument.Parse(jsonContent);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("Seller", out JsonElement seller)) {
                    sellerName = seller.GetProperty("Name").GetString();
                    sellerNip = seller.GetProperty("Nip").GetString();
                }
                if (root.TryGetProperty("Buyer", out JsonElement buyer)) {
                    buyerName = buyer.GetProperty("Name").GetString();
                    if (buyer.TryGetProperty("Identifier", out JsonElement identifier)) {
                        if (identifier.TryGetProperty("Nip", out JsonElement nip)) {
                            buyerNip = nip.GetString();
                        }
                    }
                }
                if (root.TryGetProperty("GrossAmount", out JsonElement gross)) {
                    grossAmount = gross.GetDecimal();
                }
                if (root.TryGetProperty("KsefNumber", out JsonElement ksef)) {
                    ksefNumber = ksef.GetString() ?? group.Key;
                }
            } catch (Exception ex) {
                Log.Debug($"Failed to parse JSON {jsonFile}: {ex.Message}");
            }
        }

        if ((string.IsNullOrEmpty(sellerName) || string.IsNullOrEmpty(buyerName) || grossAmount == 0) && !string.IsNullOrEmpty(xmlFile) && File.Exists(xmlFile)) {
            try {
                XDocument doc = XDocument.Load(xmlFile);
                XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

                var podmiot1 = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Podmiot1");
                var podmiot2 = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Podmiot2");

                sellerName ??= podmiot1?.Element(ns + "DaneIdentyfikacyjne")?.Element(ns + "Nazwa")?.Value;
                buyerName ??= podmiot2?.Element(ns + "DaneIdentyfikacyjne")?.Element(ns + "Nazwa")?.Value;

                sellerNip ??= podmiot1?.Element(ns + "DaneIdentyfikacyjne")?.Element(ns + "NIP")?.Value?.Replace("-", "").Replace(" ", "");
                buyerNip ??= podmiot2?.Element(ns + "DaneIdentyfikacyjne")?.Element(ns + "NIP")?.Value?.Replace("-", "").Replace(" ", "");

                var p13_1 = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "P_13_1");
                if (p13_1 != null && decimal.TryParse(p13_1.Value, out decimal gross1)) {
                    grossAmount = gross1;
                } else {
                    // Fallback for test XML or older format
                    var p13 = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "P_13");
                    if (p13 != null && decimal.TryParse(p13.Value, out decimal gross2)) {
                        grossAmount = gross2;
                    }
                }

                var p1 = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "P_1");
                if (p1 != null && DateTime.TryParse(p1.Value, out DateTime issueDate)) {
                    // Could extract date from issueDate if needed
                }
            } catch (Exception ex) {
                Log.Debug($"Failed to parse XML {xmlFile}: {ex.Message}");
            }
        }

        if (string.IsNullOrEmpty(sellerName)) sellerName = "BRAK";
        if (string.IsNullOrEmpty(buyerName)) buyerName = "BRAK";
        if (string.IsNullOrEmpty(sellerNip)) sellerNip = "";
        if (string.IsNullOrEmpty(buyerNip)) buyerNip = "";

        string sellerFirst = GetFirstWord(sellerName);
        string buyerFirst = GetFirstWord(buyerName);

        string dateDir = ExtractDateDir(ksefNumber);
        string role = DetermineRole(sellerNip, buyerNip, myNip);

        return new InvoiceData {
            KsefNumber = ksefNumber,
            SellerFirst = sellerFirst,
            BuyerFirst = buyerFirst,
            GrossAmount = grossAmount,
            DateDir = dateDir,
            Role = role
        };
    }

    internal string GetFirstWord(string name) {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "BRAK";
    }

    internal string ExtractDateDir(string ksefNumber) {
        try {
            var parts = ksefNumber.Split('-');
            if (parts.Length >= 3) {
                int year = int.Parse(parts[1]);
                int month = int.Parse(parts[2]);
                return $"{year:D4}{month:D2}";
            }
        } catch { }
        return DateTime.Now.ToString("yyyyMM");
    }

    private string DetermineRole(string sellerNip, string buyerNip, string myNip) {
        if (!string.IsNullOrEmpty(myNip)) {
            if (sellerNip == myNip) return "sprzedawca";
            if (buyerNip == myNip) return "nabywca";
        }
        return "sprzedawca";
    }

    internal string SanitizeFileName(string name) {
        foreach (char c in Path.GetInvalidFileNameChars()) {
            name = name.Replace(c, '_');
        }
        return name;
    }

    private string GetUniqueFileName(string dir, string baseName, string ext) {
        int counter = 1;
        string result;
        do {
            result = Path.Combine(dir, $"{baseName}_{counter}{ext}");
            counter++;
        } while (File.Exists(result));
        return result;
    }

    private class InvoiceData {
        public string KsefNumber { get; set; } = "";
        public string SellerFirst { get; set; } = "";
        public string BuyerFirst { get; set; } = "";
        public decimal GrossAmount { get; set; }
        public string DateDir { get; set; } = "";
        public string Role { get; set; } = "";
    }
}