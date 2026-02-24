using CommandLine;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("TestAuth", HelpText = "Authenticate using configured method")]
public class TestAuthCommand : IWithConfigCommand {
    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        await Auth(scope, cancellationToken).ConfigureAwait(false);
        return 0;
    }
}
