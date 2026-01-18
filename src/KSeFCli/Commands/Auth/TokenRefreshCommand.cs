using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using KSeFCli.Services; // Added for AuthService

namespace KSeFCli.Commands.Auth
{
    public sealed class TokenRefreshCommand : AsyncCommand<TokenRefreshCommand.Settings>
    {
        private readonly AuthService _authService;

        public sealed class Settings : CommandSettings
        {
            [CommandOption("-f|--force")]
            [Description("Force token refresh")]
            public bool ForceRefresh { get; set; }
        }

        public TokenRefreshCommand(AuthService authService)
        {
            _authService = authService;
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            var token = await _authService.RefreshTokenAsync(cancellationToken);
            if (token != null)
            {
                AnsiConsole.WriteLine("Token refreshed successfully!");
                return 0;
            }
            else
            {
                AnsiConsole.WriteLine("Failed to refresh token.");
                return 1;
            }
        }
    }
}
