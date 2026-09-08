using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace KCKSeFCli.Tests;

public class SortFakturyCommandTests {
    private static string CreateTempDir() {
        string tempDir = Path.Combine(Path.GetTempPath(), "sortfaktury_test_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void CleanupDir(string dir) {
        if (Directory.Exists(dir)) {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void GetBaseName_XmlFile_ReturnsBaseName() {
        var command = new SortFakturyCommand();
        string result = command.GetBaseName("/path/to/invoice123.xml");
        result.Should().Be("invoice123");
    }

    [Fact]
    public void GetBaseName_SummaryJsonFile_ReturnsBaseName() {
        var command = new SortFakturyCommand();
        string result = command.GetBaseName("/path/to/invoice123_summary.json");
        result.Should().Be("invoice123");
    }

    [Fact]
    public void GetBaseName_PdfFile_ReturnsBaseName() {
        var command = new SortFakturyCommand();
        string result = command.GetBaseName("/path/to/invoice123.pdf");
        result.Should().Be("invoice123");
    }

    [Fact]
    public void GetBaseName_JsonFile_ReturnsBaseName() {
        var command = new SortFakturyCommand();
        string result = command.GetBaseName("/path/to/invoice123.json");
        result.Should().Be("invoice123");
    }

    [Fact]
    public void GetFirstWord_SimpleName_ReturnsFirstWord() {
        var command = new SortFakturyCommand();
        string result = command.GetFirstWord("Jan Kowalski");
        result.Should().Be("Jan");
    }

    [Fact]
    public void GetFirstWord_SingleWord_ReturnsWord() {
        var command = new SortFakturyCommand();
        string result = command.GetFirstWord("Firma");
        result.Should().Be("Firma");
    }

    [Fact]
    public void GetFirstWord_EmptyString_ReturnsBRAK() {
        var command = new SortFakturyCommand();
        string result = command.GetFirstWord("");
        result.Should().Be("BRAK");
    }

    [Fact]
    public void GetFirstWord_WhitespaceOnly_ReturnsBRAK() {
        var command = new SortFakturyCommand();
        string result = command.GetFirstWord("   ");
        result.Should().Be("BRAK");
    }

    [Fact]
    public void ExtractDateDir_ValidKsefNumber_ReturnsYYYYMM() {
        var command = new SortFakturyCommand();
        string result = command.ExtractDateDir("123456789-2025-01-15-ABCDEF123456");
        result.Should().Be("202501");
    }

    [Fact]
    public void ExtractDateDir_InvalidKsefNumber_ReturnsCurrentDate() {
        var command = new SortFakturyCommand();
        string result = command.ExtractDateDir("invalid-format");
        result.Should().Be(DateTime.Now.ToString("yyyyMM"));
    }

    [Fact]
    public void SanitizeFileName_RemovesInvalidChars() {
        var command = new SortFakturyCommand();
        string invalidChars = new string(Path.GetInvalidFileNameChars());
        string testInput = "test" + invalidChars + "file";
        string result = command.SanitizeFileName(testInput);
        foreach (char c in Path.GetInvalidFileNameChars()) {
            result.Should().NotContain(c.ToString());
        }
    }

    [Fact]
    public async Task ExecuteAsync_DryRun_ShowsActionsWithoutChanges() {
        string tempDir = CreateTempDir();
        string outputDir = Path.Combine(tempDir, "output");
        try {
            string xmlFile = Path.Combine(tempDir, "123456789-2025-01-15-ABCDEF123456.xml");
            string jsonFile = Path.Combine(tempDir, "123456789-2025-01-15-ABCDEF123456_summary.json");

            File.WriteAllText(xmlFile, """
                <Invoice>
                    <Podmiot1><DaneIdentyfikacyjne><Nazwa>Jan Kowalski</Nazwa></DaneIdentyfikacyjne></Podmiot1>
                    <Podmiot2><DaneIdentyfikacyjne><Nazwa>Anna Nowak</Nazwa></DaneIdentyfikacyjne></Podmiot2>
                    <P_13>1230.00</P_13>
                </Invoice>
                """);

            var summary = new {
                Seller = new { Name = "Jan Kowalski" },
                Buyer = new { Name = "Anna Nowak" },
                GrossAmount = 1230.00m,
                KsefNumber = "123456789-2025-01-15-ABCDEF123456"
            };
            File.WriteAllText(jsonFile, JsonSerializer.Serialize(summary));

            var command = new SortFakturyCommand {
                OutputDir = outputDir,
                DryRun = true,
                NoMove = true,
                InputFiles = [xmlFile]
            };

            int result = await command.ExecuteAsync(CancellationToken.None);
            result.Should().Be(0);

            Directory.Exists(outputDir).Should().BeFalse();
            File.Exists(xmlFile).Should().BeTrue();
            File.Exists(jsonFile).Should().BeTrue();
        } finally {
            CleanupDir(tempDir);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CopyMode_CopiesFilesToStructuredDirectory() {
        string tempDir = CreateTempDir();
        string outputDir = Path.Combine(tempDir, "output");
        try {
            string xmlFile = Path.Combine(tempDir, "123456789-2025-01-15-ABCDEF123456.xml");
            string jsonFile = Path.Combine(tempDir, "123456789-2025-01-15-ABCDEF123456_summary.json");
            string pdfFile = Path.Combine(tempDir, "123456789-2025-01-15-ABCDEF123456.pdf");

            File.WriteAllText(xmlFile, """
                <Invoice>
                    <Podmiot1><DaneIdentyfikacyjne><Nazwa>Jan Kowalski</Nazwa></DaneIdentyfikacyjne></Podmiot1>
                    <Podmiot2><DaneIdentyfikacyjne><Nazwa>Anna Nowak</Nazwa></DaneIdentyfikacyjne></Podmiot2>
                    <P_13>1230.00</P_13>
                </Invoice>
                """);

            var summary = new {
                Seller = new { Name = "Jan Kowalski" },
                Buyer = new { Name = "Anna Nowak" },
                GrossAmount = 1230.00m,
                KsefNumber = "123456789-2025-01-15-ABCDEF123456"
            };
            File.WriteAllText(jsonFile, JsonSerializer.Serialize(summary));
            File.WriteAllText(pdfFile, "PDF content");

            var command = new SortFakturyCommand {
                OutputDir = outputDir,
                DryRun = false,
                NoMove = true,
                IncludePdf = true,
                IncludeJson = true,
                IncludeXml = true,
                InputFiles = [xmlFile, jsonFile, pdfFile]
            };

            int result = await command.ExecuteAsync(CancellationToken.None);
            result.Should().Be(0);

            string expectedDir = Path.Combine(outputDir, "sprzedawca", "202501");
            Directory.Exists(expectedDir).Should().BeTrue();

            var files = Directory.GetFiles(expectedDir);
            files.Should().HaveCount(3);
            files.Should().Contain(f => f.EndsWith(".xml"));
            files.Should().Contain(f => f.EndsWith(".json"));
            files.Should().Contain(f => f.EndsWith(".pdf"));

            foreach (var f in files) {
                Path.GetFileName(f).Should().StartWith("123456789-2025-01-15-ABCDEF123456");
                Path.GetFileName(f).Should().Contain("Jan");
                Path.GetFileName(f).Should().Contain("Anna");
                Path.GetFileName(f).Should().Contain("1230.00");
            }

            File.Exists(xmlFile).Should().BeTrue();
            File.Exists(jsonFile).Should().BeTrue();
            File.Exists(pdfFile).Should().BeTrue();
        } finally {
            CleanupDir(tempDir);
        }
    }

    [Fact]
    public async Task ExecuteAsync_MoveMode_MovesFilesToStructuredDirectory() {
        string tempDir = CreateTempDir();
        string outputDir = Path.Combine(tempDir, "output");
        try {
            string xmlFile = Path.Combine(tempDir, "123456789-2025-01-15-ABCDEF123456.xml");
            string jsonFile = Path.Combine(tempDir, "123456789-2025-01-15-ABCDEF123456_summary.json");

            File.WriteAllText(xmlFile, """
                <Invoice>
                    <Podmiot1><DaneIdentyfikacyjne><Nazwa>Jan Kowalski</Nazwa></DaneIdentyfikacyjne></Podmiot1>
                    <Podmiot2><DaneIdentyfikacyjne><Nazwa>Anna Nowak</Nazwa></DaneIdentyfikacyjne></Podmiot2>
                    <P_13>1230.00</P_13>
                </Invoice>
                """);

            var summary = new {
                Seller = new { Name = "Jan Kowalski" },
                Buyer = new { Name = "Anna Nowak" },
                GrossAmount = 1230.00m,
                KsefNumber = "123456789-2025-01-15-ABCDEF123456"
            };
            File.WriteAllText(jsonFile, JsonSerializer.Serialize(summary));

            var command = new SortFakturyCommand {
                OutputDir = outputDir,
                DryRun = false,
                NoMove = false,
                IncludePdf = false,
                IncludeJson = true,
                IncludeXml = true,
                InputFiles = [xmlFile, jsonFile]
            };

            int result = await command.ExecuteAsync(CancellationToken.None);
            result.Should().Be(0);

            string expectedDir = Path.Combine(outputDir, "sprzedawca", "202501");
            Directory.Exists(expectedDir).Should().BeTrue();

            var files = Directory.GetFiles(expectedDir);
            files.Should().HaveCount(2);

            File.Exists(xmlFile).Should().BeFalse();
            File.Exists(jsonFile).Should().BeFalse();
        } finally {
            CleanupDir(tempDir);
        }
    }

    [Fact]
    public async Task ExecuteAsync_MultipleInvoices_ProcessesAll() {
        string tempDir = CreateTempDir();
        string outputDir = Path.Combine(tempDir, "output");
        try {
            var inputFiles = new List<string>();
            for (int i = 1; i <= 3; i++) {
                string ksefNum = $"11111111{i}-2025-0{i}-15-ABCDEF123456";
                string xmlFile = Path.Combine(tempDir, $"{ksefNum}.xml");
                string jsonFile = Path.Combine(tempDir, $"{ksefNum}_summary.json");

                File.WriteAllText(xmlFile, $"""
                    <Invoice>
                        <Podmiot1><DaneIdentyfikacyjne><Nazwa>Sprzedawca {i}</Nazwa></DaneIdentyfikacyjne></Podmiot1>
                        <Podmiot2><DaneIdentyfikacyjne><Nazwa>Nabywca {i}</Nazwa></DaneIdentyfikacyjne></Podmiot2>
                        <P_13>{1000 + i * 100}.00</P_13>
                    </Invoice>
                    """);

                var summary = new {
                    Seller = new { Name = $"Sprzedawca {i}" },
                    Buyer = new { Name = $"Nabywca {i}" },
                    GrossAmount = 1000 + i * 100,
                    KsefNumber = ksefNum
                };
                File.WriteAllText(jsonFile, JsonSerializer.Serialize(summary));

                inputFiles.Add(xmlFile);
                inputFiles.Add(jsonFile);
            }

            var command = new SortFakturyCommand {
                OutputDir = outputDir,
                DryRun = false,
                NoMove = true,
                InputFiles = inputFiles
            };

            int result = await command.ExecuteAsync(CancellationToken.None);
            result.Should().Be(0);

            var allFiles = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories);
            allFiles.Should().HaveCount(6);
        } finally {
            CleanupDir(tempDir);
        }
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateNames_AppendsCounter() {
        string tempDir = CreateTempDir();
        string outputDir = Path.Combine(tempDir, "output");
        try {
            string ksefNum = "123456789-2025-01-15-ABCDEF123456";
            string xmlFile1 = Path.Combine(tempDir, $"{ksefNum}.xml");
            string xmlFile2 = Path.Combine(tempDir, $"{ksefNum}_2.xml");
            string jsonFile1 = Path.Combine(tempDir, $"{ksefNum}_summary.json");
            string jsonFile2 = Path.Combine(tempDir, $"{ksefNum}_2_summary.json");

            string xmlContent = """
                <Invoice>
                    <Podmiot1><DaneIdentyfikacyjne><Nazwa>Jan Kowalski</Nazwa></DaneIdentyfikacyjne></Podmiot1>
                    <Podmiot2><DaneIdentyfikacyjne><Nazwa>Anna Nowak</Nazwa></DaneIdentyfikacyjne></Podmiot2>
                    <P_13>1230.00</P_13>
                </Invoice>
                """;
            File.WriteAllText(xmlFile1, xmlContent);
            File.WriteAllText(xmlFile2, xmlContent);

            var summary = new {
                Seller = new { Name = "Jan Kowalski" },
                Buyer = new { Name = "Anna Nowak" },
                GrossAmount = 1230.00m,
                KsefNumber = ksefNum
            };
            File.WriteAllText(jsonFile1, JsonSerializer.Serialize(summary));
            File.WriteAllText(jsonFile2, JsonSerializer.Serialize(summary));

            var command = new SortFakturyCommand {
                OutputDir = outputDir,
                DryRun = false,
                NoMove = true,
                InputFiles = [xmlFile1, xmlFile2, jsonFile1, jsonFile2]
            };

            int result = await command.ExecuteAsync(CancellationToken.None);
            result.Should().Be(0);

            var files = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories);
            files.Should().HaveCount(4);

            var fileNames = files.Select(Path.GetFileName).ToList();
            fileNames.Should().Contain(f => f.Contains("_1"));
        } finally {
            CleanupDir(tempDir);
        }
    }

    [Fact]
    public async Task ExecuteAsync_InputFilesSpecified_ProcessesOnlyThose() {
        string tempDir = CreateTempDir();
        string outputDir = Path.Combine(tempDir, "output");
        try {
            string xmlFile1 = Path.Combine(tempDir, "111111111-2025-01-15-ABCDEF123456.xml");
            string xmlFile2 = Path.Combine(tempDir, "222222222-2025-01-15-ABCDEF123456.xml");
            string jsonFile1 = Path.Combine(tempDir, "111111111-2025-01-15-ABCDEF123456_summary.json");
            string jsonFile2 = Path.Combine(tempDir, "222222222-2025-01-15-ABCDEF123456_summary.json");

            File.WriteAllText(xmlFile1, """
                <Invoice>
                    <Podmiot1><DaneIdentyfikacyjne><Nazwa>Seller1</Nazwa></DaneIdentyfikacyjne></Podmiot1>
                    <Podmiot2><DaneIdentyfikacyjne><Nazwa>Buyer1</Nazwa></DaneIdentyfikacyjne></Podmiot2>
                    <P_13>100.00</P_13>
                </Invoice>
                """);
            File.WriteAllText(xmlFile2, """
                <Invoice>
                    <Podmiot1><DaneIdentyfikacyjne><Nazwa>Seller2</Nazwa></DaneIdentyfikacyjne></Podmiot1>
                    <Podmiot2><DaneIdentyfikacyjne><Nazwa>Buyer2</Nazwa></DaneIdentyfikacyjne></Podmiot2>
                    <P_13>200.00</P_13>
                </Invoice>
                """);

            var summary1 = new { Seller = new { Name = "Seller1" }, Buyer = new { Name = "Buyer1" }, GrossAmount = 100.00m, KsefNumber = "111111111-2025-01-15-ABCDEF123456" };
            var summary2 = new { Seller = new { Name = "Seller2" }, Buyer = new { Name = "Buyer2" }, GrossAmount = 200.00m, KsefNumber = "222222222-2025-01-15-ABCDEF123456" };
            File.WriteAllText(jsonFile1, JsonSerializer.Serialize(summary1));
            File.WriteAllText(jsonFile2, JsonSerializer.Serialize(summary2));

            var command = new SortFakturyCommand {
                OutputDir = outputDir,
                DryRun = false,
                NoMove = true,
                InputFiles = [xmlFile1, jsonFile1]
            };

            int result = await command.ExecuteAsync(CancellationToken.None);
            result.Should().Be(0);

            var files = Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories);
            files.Should().HaveCount(2);
            files.Should().AllSatisfy(f => Path.GetFileName(f).Should().Contain("111111111"));
        } finally {
            CleanupDir(tempDir);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NoFilesFound_ReturnsZero() {
        string tempDir = CreateTempDir();
        string outputDir = Path.Combine(tempDir, "output");
        try {
            var command = new SortFakturyCommand {
                OutputDir = outputDir,
                DryRun = false
            };

            int result = await command.ExecuteAsync(CancellationToken.None);
            result.Should().Be(0);
        } finally {
            CleanupDir(tempDir);
        }
    }
}