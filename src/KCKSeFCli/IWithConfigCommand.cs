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

    [Option("nip", HelpText = "Taxpayer NIP")]
    public string? CmdNip { get; set; }

    [Option("token", HelpText = "Authentication token")]
    public string? CmdToken { get; set; }

    [Option("private-key-file", HelpText = "Path to the private key file")]
    public string? CmdPrivateKeyFile { get; set; }

    [Option("certificate-file", HelpText = "Path to the certificate file")]
    public string? CmdCertificateFile { get; set; }

    [Option("password-env", HelpText = "Environment variable containing the password for the private key")]
    public string? CmdPasswordEnv { get; set; }

    private readonly Lazy<ProfileConfigWithName> _cachedProfile;
    private readonly Lazy<TokenStore> _tokenStore;

    public IWithConfigCommand()
    {
        _cachedProfile = new Lazy<ProfileConfigWithName>(() =>
        {
            bool anyCmdOptionSet = !string.IsNullOrEmpty(CmdEnvironment) ||
                                   !string.IsNullOrEmpty(CmdNip) ||
                                   !string.IsNullOrEmpty(CmdToken) ||
                                   !string.IsNullOrEmpty(CmdPrivateKeyFile) ||
                                   !string.IsNullOrEmpty(CmdCertificateFile) ||
                                   !string.IsNullOrEmpty(CmdPasswordEnv);

            if (anyCmdOptionSet)
            {
                if (ConfigFile is not null || ActiveProfile is not null)
                {
                    throw new InvalidOperationException("Cannot use --config or --active with command-line profile options.");
                }

                bool isTokenAuth = !string.IsNullOrEmpty(CmdToken);
                bool isCertAuth = !string.IsNullOrEmpty(CmdPrivateKeyFile) || !string.IsNullOrEmpty(CmdCertificateFile) || !string.IsNullOrEmpty(CmdPasswordEnv);

                if (isTokenAuth && isCertAuth)
                {
                    throw new InvalidOperationException("Cannot use --token with certificate-related options (--private-key-file, --certificate-file, --password-env).");
                }

                var profile = new ProfileConfig
                {
                    Environment = CmdEnvironment,
                    Nip = CmdNip,
                    Token = CmdToken,
                    Certificate = (string.IsNullOrEmpty(CmdCertificateFile) || string.IsNullOrEmpty(CmdPrivateKeyFile)) ? null : new CertificateConfig
                    {
                        Certificate = System.IO.File.ReadAllText(CmdCertificateFile),
                        Private_Key = System.IO.File.ReadAllText(CmdPrivateKeyFile),
                        Password = string.IsNullOrEmpty(CmdPasswordEnv) ? "" : System.Environment.GetEnvironmentVariable(CmdPasswordEnv) ?? "",
                    }
                };
                return new ProfileConfigWithName(profile, "cmd");
            }
            else
            {
                // Resolve actualConfigFile: CLI -> ENV -> Hardcoded Default
                string? actualConfigFileToLoad = ConfigFile;
                if (string.IsNullOrEmpty(actualConfigFileToLoad))
                {
                    actualConfigFileToLoad = System.Environment.GetEnvironmentVariable("KCKSEFCLI_CONFIG") ?? "";
                }
                if (string.IsNullOrEmpty(actualConfigFileToLoad))
                {
                    actualConfigFileToLoad = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".config", "kcksefcli", "kcksefcli.yaml");
                }

                // Resolve actualActiveProfile: CLI -> ENV
                string? actualActiveProfileToLoad = ActiveProfile;
                if (string.IsNullOrEmpty(actualActiveProfileToLoad))
                {
                    actualActiveProfileToLoad = System.Environment.GetEnvironmentVariable("KCKSEFCLI_ACTIVE") ?? "";
                }

                var config = ConfigLoader.Load(actualConfigFileToLoad!, actualActiveProfileToLoad!);
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

