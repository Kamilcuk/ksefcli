using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace KCKSeFCli.Tests;

public class XML2JSONCommandTests {
    [Fact]
    public void XElementToObject_SimpleElement_ReturnsValue() {
        var element = XElement.Parse("<Test>value</Test>");
        var result = XML2JSONCommand.XElementToObject(element);
        result.Should().Be("value");
    }

    [Fact]
    public void XElementToObject_ElementWithAttributes_ReturnsDict() {
        var element = XElement.Parse("<Test attr='value'>content</Test>");
        var result = XML2JSONCommand.XElementToObject(element);
        var dict = result.Should().BeOfType<Dictionary<string, object>>().Subject;
        dict["@attr"].Should().Be("value");
        dict["#text"].Should().Be("content");
    }

    [Fact]
    public void XElementToObject_NestedElements_ReturnsNestedDict() {
        var element = XElement.Parse("<Root><Child>value</Child></Root>");
        var result = XML2JSONCommand.XElementToObject(element);
        var dict = result.Should().BeOfType<Dictionary<string, object>>().Subject;
        dict["Child"].Should().Be("value");
    }

    [Fact]
    public void XElementToObject_MultipleSameNameElements_ReturnsList() {
        var element = XElement.Parse("<Root><Item>1</Item><Item>2</Item></Root>");
        var result = XML2JSONCommand.XElementToObject(element);
        var dict = result.Should().BeOfType<Dictionary<string, object>>().Subject;
        dict["Item"].Should().BeOfType<List<object>>().Which.Should().HaveCount(2);
    }

    [Fact]
    public void ExecuteAsync_ValidXmlFile_WritesJson() {
        string tempDir = Path.Combine(Path.GetTempPath(), "xml2json_test_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        try {
            string xmlFile = Path.Combine(tempDir, "test.xml");
            string jsonFile = Path.Combine(tempDir, "test.json");
            File.WriteAllText(xmlFile, "<Invoice><Number>123</Number><Amount>100.50</Amount></Invoice>");

            var command = new XML2JSONCommand {
                InputFile = xmlFile,
                OutputFile = jsonFile,
                Indent = true
            };

            var result = command.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
            result.Should().Be(0);
            File.Exists(jsonFile).Should().BeTrue();

            string json = File.ReadAllText(jsonFile);
            json.Should().Contain("\"Number\": \"123\"");
            json.Should().Contain("\"Amount\": \"100.50\"");
        } finally {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ExecuteAsync_NonExistentFile_ReturnsError() {
        var command = new XML2JSONCommand {
            InputFile = "/nonexistent/file.xml"
        };
        var result = command.ExecuteAsync(CancellationToken.None).GetAwaiter().GetResult();
        result.Should().Be(1);
    }
}