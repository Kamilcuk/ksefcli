using KSeF.Client.Api.Services;
using KSeF.Client.Api.Services.Internal;
using KSeF.Client.Clients;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using Spectre.Console.Cli;


namespace KSeFCli;

public class TokenAuthCommand : AsyncCommand<TokenAuthCommand.Settings> {
    public class Settings : GlobalSettings {
        [CommandOption("--nip")]
        public string Nip { get; set; } = null!;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings) {
        IKSeFClient ksefClient = KSeFClientFactory.CreateKSeFClient(settings);

        AuthenticationChallengeResponse challenge = await ksefClient.GetAuthChallengeAsync().ConfigureAwait(false);
        long timestampMs = challenge.Timestamp.ToUnixTimeMilliseconds();


        string ksefToken = settings.Token;
        Console.WriteLine("1. Przygotowanie i szyfrowanie tokena");
        // Przygotuj "token|timestamp" i zaszyfruj RSA-OAEP SHA-256 zgodnie z wymaganiem API
        string tokenWithTimestamp = $"{ksefToken}|{timestampMs}";
        byte[] tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenWithTimestamp);
        CryptographyClient a = new CryptographyClient(KSeFClientFactory.CreateRestclient(settings));
        DefaultCertificateFetcher i = new DefaultCertificateFetcher(a);
        CryptographyService crypto = new CryptographyService(i);
        byte[] encrypted = crypto.EncryptKsefTokenWithRSAUsingPublicKey(tokenBytes);
        string encryptedTokenB64 = Convert.ToBase64String(encrypted);

        Console.WriteLine("2. Wysłanie żądania uwierzytelnienia tokenem KSeF");
        AuthenticationKsefTokenRequest request = new AuthenticationKsefTokenRequest {
            Challenge = challenge.Challenge,
            ContextIdentifier = new AuthenticationTokenContextIdentifier {
                Type = AuthenticationTokenContextIdentifierType.Nip,
                Value = settings.Nip
            },
            EncryptedToken = encryptedTokenB64,
            AuthorizationPolicy = null
        };

        SignatureResponse signature = await ksefClient.SubmitKsefTokenAuthRequestAsync(request, new CancellationToken()).ConfigureAwait(false);

        Console.WriteLine("3. Sprawdzenie statusu uwierzytelniania");
        DateTime startTime = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMinutes(2);
        AuthStatus status;
        do {
            status = await ksefClient.GetAuthStatusAsync(signature.ReferenceNumber, signature.AuthenticationToken.Token).ConfigureAwait(false);
            Console.WriteLine($"      Status: {status.Status.Code} - {status.Status.Description} | upłynęło: {DateTime.UtcNow - startTime:mm\\:ss}");
            if (status.Status.Code != 200) {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }
        while (status.Status.Code == 100 && (DateTime.UtcNow - startTime) < timeout);

        if (status.Status.Code != 200) {
            Console.WriteLine("[!] Uwierzytelnienie nie powiodło się lub przekroczono czas oczekiwania.");
            Console.WriteLine($"    Kod: {status.Status.Code}, Opis: {status.Status.Description}");
            return 1;
        }

        Console.WriteLine("4. Uzyskanie tokena dostępowego (accessToken)");
        AuthenticationOperationStatusResponse tokenResponse = await ksefClient.GetAccessTokenAsync(signature.AuthenticationToken.Token).ConfigureAwait(false);

        string accessToken = tokenResponse.AccessToken?.Token ?? string.Empty;
        string refreshToken = tokenResponse.RefreshToken?.Token ?? string.Empty;
        Console.WriteLine($"    AccessToken: {accessToken}");
        Console.WriteLine($"    RefreshToken: {refreshToken}");

        Console.WriteLine("Zakończono pomyślnie.");
        return 0;
    }
}
