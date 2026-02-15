using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using CommandLine;

using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Api.Services;
using KSeF.Client.ClientFactory;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.DI;
using KSeF.Client.Extensions;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

public abstract class IWithConfigCommand : IGlobalCommand
{
    [Option('c', "config", HelpText = "Path to config file")]
    public string ConfigFile { get; set; } = "";

    [Option('a', "active", HelpText = "Active profile name")]
    public string ActiveProfile { get; set; } = "";

    [Option("cache", HelpText = "Path to token cache file")]
    public string TokenCache { get; set; } = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".cache", "kcksefcli", "tokenstore.json");

    [Option("no-tokencache", HelpText = "Disable token cache usage")]
    public bool NoTokenCache { get; set; }

    [Option("environment", HelpText = "KSeF environment")]
    public string? CmdEnvironment { get; set; }

    [Option("token", HelpText = "Authentication token")]
    public string? CmdToken { get; set; }

    private readonly Lazy<ProfileConfigWithName> _cachedProfile;
    private readonly Lazy<TokenStore> _tokenStore;

    public IWithConfigCommand()
    {
        _cachedProfile = new Lazy<ProfileConfigWithName>(() =>
        {
            bool anyCmdOptionSet = !string.IsNullOrEmpty(CmdEnvironment) ||
                                   isTokenAuth;

            if (anyCmdOptionSet)
            {
                // Resolve config from command line arguments
                if (ConfigFile != "" || ActiveProfile != "")
                {
                    throw new InvalidOperationException("Cannot use --config or --active with command-line profile options.");
                }
                if (string.IsNullOrEmpty(CmdEnvironment))
                {
                    throw new InvalidOperationException("You have to use --environment is specifying authentication on command line with --token).");
                }

                string? nipToUse = null;
                if (!string.IsNullOrEmpty(CmdToken))
                {
                    nipToUse = CheckNip.ExtractNipFromToken(CmdToken);
                }

                if (string.IsNullOrEmpty(nipToUse))
                {
                    throw new InvalidOperationException("You have to specify a token that contains nip.");
                }

                if (!CheckNip.IsNipValid(nipToUse))
                {
                    throw new InvalidOperationException($"Invalid NIP format: {nipToUse}");
                }

                var profile = new ProfileConfig
                {
                    Environment = CmdEnvironment,
                    Nip = nipToUse,
                    Token = CmdToken,
                };
                return new ProfileConfigWithName(profile, "cmd");
            }
            else
            {
                // Resolve config from file
                string configFileDefault = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".config", "kcksefcli", "kcksefcli.yaml");
                string? configEnv = System.Environment.GetEnvironmentVariable("KCKSEFCLI_CONFIG");
                string actualConfigFileToLoad = !string.IsNullOrEmpty(ConfigFile) ? ConfigFile : !string.IsNullOrEmpty(configEnv) ? configEnv : configFileDefault;

                string? profileEnv = System.Environment.GetEnvironmentVariable("KCKSEFCLI_ACTIVE");
                string actualActiveProfileToLoad = !string.IsNullOrEmpty(ActiveProfile) ? ActiveProfile : !string.IsNullOrEmpty(profileEnv) ? profileEnv : "";

                var config = ConfigLoader.Load(actualConfigFileToLoad, actualActiveProfileToLoad);
                var profile = config.Profiles[config.ActiveProfile];
                return new ProfileConfigWithName(profile, config.ActiveProfile);
            }
        });
        _tokenStore = new Lazy<TokenStore>(() => new TokenStore(TokenCache));
    }

    protected TokenStore GetTokenStore() => _tokenStore.Value;

    public ProfileConfigWithName Config() => _cachedProfile.Value;

    public TokenStore.Key GetTokenStoreKey()
    {
        var config = Config();
        return new TokenStore.Key(config.Name, config);
    }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = GetScope();
        return await ExecuteInScopeAsync(scope, cancellationToken).ConfigureAwait(false);
    }

    public abstract Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken);

    public async Task<AuthenticationOperationStatusResponse> Auth(IServiceScope scope, CancellationToken cancellationToken)
    {
        var config = Config();
        AuthenticationOperationStatusResponse response = config.AuthMethod switch
        {
            AuthMethod.KsefToken => await Authenticate.TokenAuth(config, scope, GetCryptographicService, cancellationToken).ConfigureAwait(false),
            AuthMethod.Xades => await Authenticate.CertAuth(config, scope, GetCryptographicService, cancellationToken).ConfigureAwait(false),
            _ => throw new Exception($"Invalid authmethod in profile: {config.Environment}")
        };
        Log.LogInformation($"Access token valid until: {response.AccessToken.ValidUntil} . Refresh token valid until: {response.RefreshToken.ValidUntil}");
        return response;
    }



    public async Task<string> GetAccessToken(IServiceScope scope, CancellationToken cancellationToken)
    {
        if (NoTokenCache)
        {
            Log.LogInformation("Token cache disabled, starting new auth");
            AuthenticationOperationStatusResponse response = await Auth(scope, cancellationToken).ConfigureAwait(false);
            return response.AccessToken.Token;
        }

        TokenStore tokenStore = GetTokenStore();
        TokenStore.Key key = GetTokenStoreKey();
        TokenStore.Data? storedToken = tokenStore.GetToken(key);

        if (storedToken == null || storedToken.Response.RefreshToken.ValidUntil < DateTime.UtcNow.AddMinutes(1))
        {
            Log.LogInformation("No valid token found in store, starting new auth");
            AuthenticationOperationStatusResponse response = await Auth(scope, cancellationToken).ConfigureAwait(false);
            tokenStore.SetToken(key, new TokenStore.Data(response));
            return response.AccessToken.Token;
        }

        if (storedToken.Response.AccessToken.ValidUntil < DateTime.UtcNow.AddMinutes(10))
        {
            Log.LogInformation("Refreshing token");
            AuthenticationOperationStatusResponse refreshedResponse = await TokenRefresh(scope, storedToken.Response.RefreshToken, cancellationToken).ConfigureAwait(false);
            tokenStore.SetToken(key, new TokenStore.Data(refreshedResponse));
            return refreshedResponse.AccessToken.Token;
        }

        return storedToken.Response.AccessToken.Token;
    }

    public async Task<AuthenticationOperationStatusResponse> TokenRefresh(IServiceScope scope, TokenInfo refreshToken, CancellationToken cancellationToken)
    {
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        RefreshTokenResponse response = await ksefClient.RefreshAccessTokenAsync(refreshToken.Token, cancellationToken).ConfigureAwait(false);
        return new AuthenticationOperationStatusResponse
        {
            AccessToken = response.AccessToken,
            RefreshToken = refreshToken,
        };
    }

    private IServiceScope GetScope()
    {
        var config = Config();
        IServiceCollection services = new ServiceCollection();
        KSeF.Client.ClientFactory.Environment environment = config.Environment.ToUpper() switch
        {
            "PROD" => KSeF.Client.ClientFactory.Environment.Prod,
            "DEMO" => KSeF.Client.ClientFactory.Environment.Demo,
            "TEST" => KSeF.Client.ClientFactory.Environment.Test,
            _ => throw new Exception($"Invalid environment in profile: {config.Environment}")
        };
        services.AddSingleton<ProfileConfig>((ProfileConfig)config);
        services.AddKSeFClient(options =>
        {
            options.BaseUrl = KsefEnvironmentConfig.BaseUrls[environment];
        });
        ServiceCollectionExtensions.AddCryptographyClient(services);
        ServiceProvider provider = services.BuildServiceProvider();
        IServiceScope scope = provider.CreateScope();
        return scope;
    }

    public async Task<ICryptographyService> GetCryptographicService(IServiceScope scope, CancellationToken cancellationToken)
    {
        ICryptographyService cryptographyService = scope.ServiceProvider.GetRequiredService<ICryptographyService>();
        await cryptographyService.WarmupAsync(cancellationToken).ConfigureAwait(false);
        return cryptographyService;
    }
}

