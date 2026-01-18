using Spectre.Console.Cli;
using System.ComponentModel;
using System.Threading;

namespace KSeFCli.Commands.Auth
{
    [Description("Manage KSeF authorization and tokens.")]
    public sealed class AuthCommand : Command
    {
        public override int Execute(CommandContext context, CancellationToken cancellationToken)
        {
            return 1; // Indicate that this command is a branch and expects subcommands.
        }
    }
}
