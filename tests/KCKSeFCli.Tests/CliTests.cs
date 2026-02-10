using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using KCKSeFCli;
using Xunit.Abstractions;

namespace KCKSeFCli.Tests;

public class CliTestFixture : IDisposable
{
    public string CliPath { get; private set; }
    public string ConfigPath { get; private set; }
    public string TestsDirectory { get; private set; }

    public CliTestFixture()
    {
        TestsDirectory = Path.Combine(Directory.GetCurrentDirectory());
        CliPath = Path.Combine(Directory.GetCurrentDirectory(), "kcksefcli");
        ConfigPath = Path.Combine(TestsDirectory, "test_kcksefcli.yaml");

        // Ensure the CLI executable is built and available
        // This part would typically be handled by a build script or by running `dotnet build` in CI/CD
        // For local development/testing, you might need to manually ensure it's built or add a pre-test build step.
        // For now, assuming it's built and copied to the test output directory by the .csproj settings.
        if (!File.Exists(CliPath))
        {
             var buildResult = RunCliCommand("dotnet", new[] { "build", Path.Combine(TestsDirectory, "../../src/KCKSeFCli") });
             if (buildResult.ExitCode != 0)
             {
                 throw new InvalidOperationException($"CLI build failed: {buildResult.StandardError}");
             }

            // Find the published executable - this might be brittle depending on publish output structure
            var publishPath = Path.Combine(TestsDirectory, "../../src/KCKSeFCli/bin/Debug/net10.0");
            var exeFiles = Directory.GetFiles(publishPath, "kcksefcli*");
            if (!exeFiles.Any())
            {
                throw new InvalidOperationException($"kcksefcli executable not found in {publishPath}");
            }
            CliPath = exeFiles.First();
        }

        Environment.SetEnvironmentVariable("KCKSEFCLI_CONFIG", ConfigPath);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("KCKSEFCLI_CONFIG", null);
    }

    public ProcessResult RunCliCommand(string command, IEnumerable<string> args, string? activeProfile = null, IDictionary<string, string>? environmentVariables = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = TestsDirectory // Run commands from the tests directory
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (activeProfile != null)
        {
            startInfo.ArgumentList.Add("-a");
            startInfo.ArgumentList.Add(activeProfile);
        }

        if (environmentVariables != null)
        {
            foreach (var envVar in environmentVariables)
            {
                startInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
            }
        }

        using var process = Process.Start(startInfo);
        process.Should().NotBeNull();

        string output = process!.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(output, error, process.ExitCode);
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
        var result = _fixture.RunCliCommand(_fixture.CliPath, new[] { "--version" });

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().StartWith("kcksefcli");
        _output.WriteLine(result.StandardOutput);
    }

    [Fact]
    public void Cli_Help_ShouldReturnHelpText()
    {
        // Act
        var result = _fixture.RunCliCommand(_fixture.CliPath, new[] { "--help" });

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
        var result = _fixture.RunCliCommand(_fixture.CliPath, new[] { "PrintConfig" }, activeProfile, environmentVariables);

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
        var result = _fixture.RunCliCommand(_fixture.CliPath, new[] { "SprawdzLimitCertyfikatow" }, "token_test");

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
        var result = _fixture.RunCliCommand(_fixture.CliPath, new[] { "UniewaznijCertyfikat", "--help" });

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Certificate serial number to revoke");
        _output.WriteLine(result.StandardOutput);
    }

    [Fact]
    public void Cli_WylistujCertyfikaty_Help_ShouldReturnHelpText()
    {
        // Act
        var result = _fixture.RunCliCommand(_fixture.CliPath, new[] { "WylistujCertyfikaty", "--help" });

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Filter by certificate status");
        _output.WriteLine(result.StandardOutput);
    }

    [Fact]
    public void Cli_PobierzCertyfikat_Help_ShouldReturnHelpText()
    {
        // Act
        var result = _fixture.RunCliCommand(_fixture.CliPath, new[] { "PobierzCertyfikat", "--help" });

        // Assert
        result.ExitCode.Should().Be(0);
        result.StandardOutput.Should().Contain("Certificate serial number to retrieve");
        _output.WriteLine(result.StandardOutput);
    }

    [Fact]
    public void Cli_NowyCertyfikat_Help_ShouldReturnHelpText()
    {
        // Act
        var result = _fixture.RunCliCommand(_fixture.CliPath, new[] { "NowyCertyfikat", "--help" });

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
