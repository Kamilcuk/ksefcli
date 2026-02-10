using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using KCKSeFCli;
using Xunit.Abstractions;

namespace KCKSeFCli.Tests;

public class CliTestFixture : IDisposable
{
    public string ConfigPath { get; private set; }
    public string TestsDirectory { get; private set; }

    private TextWriter _originalConsoleOut;
    private TextWriter _originalConsoleError;

    public CliTestFixture()
    {
        TestsDirectory = Path.Combine(Directory.GetCurrentDirectory());
        ConfigPath = Path.Combine(TestsDirectory, "test_kcksefcli.yaml");

        _originalConsoleOut = Console.Out;
        _originalConsoleError = Console.Error;

        Environment.SetEnvironmentVariable("KCKSEFCLI_CONFIG", ConfigPath);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("KCKSEFCLI_CONFIG", null);
        Console.SetOut(_originalConsoleOut);
        Console.SetError(_originalConsoleError);
    }

    public ProcessResult RunCliCommand(IEnumerable<string> args, string? activeProfile = null, IDictionary<string, string>? environmentVariables = null)
    {
        using var swOut = new StringWriter();
        using var swErr = new StringWriter();

        Console.SetOut(swOut);
        Console.SetError(swErr);

        var allArgs = new List<string>(args);

        if (activeProfile != null)
        {
            allArgs.Insert(0, "-a");
            allArgs.Insert(1, activeProfile);
        }

        // Temporarily set environment variables for this run
        var originalEnvironmentVariables = new Dictionary<string, string?>();
        if (environmentVariables != null)
        {
            foreach (var envVar in environmentVariables)
            {
                originalEnvironmentVariables[envVar.Key] = Environment.GetEnvironmentVariable(envVar.Key);
                Environment.SetEnvironmentVariable(envVar.Key, envVar.Value);
            }
        }

        int exitCode = KCKSeFCli.Program.Main(allArgs.ToArray()).GetAwaiter().GetResult();

        // Restore original environment variables
        foreach (var envVar in originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(envVar.Key, envVar.Value);
        }

        return new ProcessResult(swOut.ToString(), swErr.ToString(), exitCode);
    }

    public record ProcessResult(string StandardOutput, string StandardError, int ExitCode);
}

[CollectionDefinition("CliTests")]
public class CliTestCollection : ICollectionFixture<CliTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture interfaces.
}

[Collection("CliTests")]
public class CliTests
{
    private readonly CliTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public CliTests(CliTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public void Cli_Version_ShouldReturnCorrectVersion()
    {
        // Act
        var result = _fixture.RunCliCommand(new[] { "--version" });

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().StartWith("kcksefcli");
        _output.WriteLine(result.StandardOutput);
    }

    [Fact]
    public void Cli_Help_ShouldReturnHelpText()
    {
        // Act
        var result = _fixture.RunCliCommand(new[] { "--help" });

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("GetFaktura");
        result.StandardOutput.Should().Contain("SprawdzLimitCertyfikatow");
        _output.WriteLine(result.StandardOutput);
    }

    [Theory]
    [InlineData("cert_test")]
    [InlineData("token_test")]
    [InlineData("cert_env_password_test", "env_password")]
    [InlineData("cert_inline_test")]
    public void Cli_PrintConfig_ShouldPrintCorrectConfig(string activeProfile, string? passwordEnv = null)
    {
        // Arrange
        var environmentVariables = new Dictionary<string, string>();
        if (passwordEnv != null)
        {
            environmentVariables["TEST_PASSWORD_ENV"] = passwordEnv;
        }

        // Act
        var result = _fixture.RunCliCommand(new[] { "PrintConfig" }, activeProfile, environmentVariables);

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain($"environment: test");
        result.StandardOutput.Should().Contain($"active_profile: {activeProfile}");
        _output.WriteLine(result.StandardOutput);
    }

    [Fact]
    public void Cli_SprawdzLimitCertyfikatow_ShouldReturnJson()
    {
        // Act
        var result = _fixture.RunCliCommand(new[] { "SprawdzLimitCertyfikatow" }, "token_test");

        // Assert
        result.ExitCode.Should().Be(0);
        Action act = () => JsonDocument.Parse(result.StandardOutput);
        act.Should().NotThrow();
        _output.WriteLine(result.StandardOutput);
    }

    [Fact]
    public void Cli_UniewaznijCertyfikat_Help_ShouldReturnHelpText()
    {
        // Act
        var result = _fixture.RunCliCommand(new[] { "UniewaznijCertyfikat", "--help" });

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Certificate serial number to revoke");
        _output.WriteLine(result.StandardOutput);
    }

    [Fact]
    public void Cli_WylistujCertyfikaty_Help_ShouldReturnHelpText()
    {
        // Act
        var result = _fixture.RunCliCommand(new[] { "WylistujCertyfikaty", "--help" });

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Filter by certificate status");
        _output.WriteLine(result.StandardOutput);
    }

    [Fact]
    public void Cli_PobierzCertyfikat_Help_ShouldReturnHelpText()
    {
        // Act
        var result = _fixture.RunCliCommand(new[] { "PobierzCertyfikat", "--help" });

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Certificate serial number to retrieve");
        _output.WriteLine(result.StandardOutput);
    }

    [Fact]
    public void Cli_NowyCertyfikat_Help_ShouldReturnHelpText()
    {
        // Act
        var result = _fixture.RunCliCommand(new[] { "NowyCertyfikat", "--help" });

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Name for the new certificate");
        _output.WriteLine(result.StandardOutput);
    }

    // [Fact]
    // public void Cli_SzukajFaktur_ShouldReturnOneInvoice()
    // {
    //     // Act
    //     var result = _fixture.RunCliCommand(_fixture.CliPath, new[] { "SzukajFaktur", "--from", "2026-01-21T00:00:00+01:00", "--to", "2026-01-22T00:00:00+01:00" }, "token_test");

    //     // Assert
    //     result.ExitCode.Should().Be(0);
    //     var doc = JsonDocument.Parse(result.StandardOutput);
    //     doc.RootElement.EnumerateArray().Should().HaveCount(1);
    //     _output.WriteLine(result.StandardOutput);
    // }
}
