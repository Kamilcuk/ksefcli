using KSeF.Client.ClientFactory;
using KSeF.Client.Core.Interfaces.Clients;
using Spectre.Console.Cli;


namespace KSeFCli;

public abstract class BaseKsefCommand<TSettings> : AsyncCommand<TSettings> where TSettings : GlobalSettings
{
    private readonly IKSeFClientFactory _ksefClientFactory;

    protected BaseKsefCommand(IKSeFClientFactory ksefClientFactory)
    {
        _ksefClientFactory = ksefClientFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        KsefConfig config = KsefConfigLoader.Load(settings.ConfigPath, settings.ActiveProfileNameOverride);
        ProfileConfig activeProfile = config.Profiles[config.ActiveProfile];
        settings.ActiveProfile = activeProfile;

        KSeF.Client.ClientFactory.Environment environment = activeProfile.Environment switch
        {
            "PROD" => KSeF.Client.ClientFactory.Environment.Prod,
            "DEMO" => KSeF.Client.ClientFactory.Environment.Demo,
            "TEST" => KSeF.Client.ClientFactory.Environment.Test,
            _ => KSeF.Client.ClientFactory.Environment.Test
        };

        IKSeFClient client = _ksefClientFactory.KSeFClient(environment);

        return await ExecuteWithProfileAsync(context, settings, activeProfile, client, cancellationToken).ConfigureAwait(false);
    }

    public abstract Task<int> ExecuteWithProfileAsync(CommandContext context, TSettings settings, ProfileConfig profile, IKSeFClient client, CancellationToken cancellationToken);
}
