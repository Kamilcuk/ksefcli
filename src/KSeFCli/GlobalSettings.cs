using System.ComponentModel;
using Spectre.Console.Cli;

namespace KSeFCli;

public class GlobalSettings : CommandSettings
{
    [CommandOption("-c|--config")]
    [Description("Path to the YAML configuration file.")]
    public string ConfigPath { get; set; } = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "ksefcli", "ksefcli.yaml");

    [CommandOption("-a|--active")]
    [Description("Overrides the active profile name from the config file.")]
    public string? ActiveProfileNameOverride { get; set; }

    public ProfileConfig? ActiveProfile { get; set; }
}

