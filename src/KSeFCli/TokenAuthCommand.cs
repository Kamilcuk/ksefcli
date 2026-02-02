using System.Text.Json;

using CommandLine;

namespace KSeFCli;

[Verb("TokenAuth", HelpText = "Authenticate using a KSeF token")]
public class TokenAuthCommand : IWithConfigCommand
{
    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var tokenResponse = await TokenAuth(cancellationToken).ConfigureAwait(false);
        Console.Out.WriteLine(JsonSerializer.Serialize(tokenResponse));
        return 0;
    }
}
