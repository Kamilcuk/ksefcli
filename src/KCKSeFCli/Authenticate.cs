using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Api.Services;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Extensions;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

public static class Authenticate
{
    public static async Task<AuthenticationOperationStatusResponse> TokenAuth(
        ProfileConfig config,
        IServiceScope scope,
        Func<IServiceScope, CancellationToken, Task<ICryptographyService>> GetCryptographicService,
        CancellationToken cancellationToken)
    {
        if (config.AuthMethod != AuthMethod.KsefToken)
        {
            throw new InvalidOperationException("This command requires token authentication.");
        }

        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        ICryptographyService cryptographyService = await GetCryptographicService(scope, cancellationToken).ConfigureAwait(false);

        Log.LogInformation("1. Getting challenge");
        AuthenticationChallengeResponse challenge = await ksefClient.GetAuthChallengeAsync().ConfigureAwait(false);
        long timestampMs = challenge.Timestamp.ToUnixTimeMilliseconds();
        string ksefToken = config.Token ?? throw new InvalidOperationException("KSeF token is missing");
        Log.LogInformation("1. Przygotowanie i szyfrowanie tokena");
        string tokenWithTimestamp = $"{ksefToken}|{timestampMs}";
        byte[] tokenBytes = System.Text.Encoding.UTF8.GetBytes(tokenWithTimestamp);
        byte[] encrypted = cryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(tokenBytes);
        string encryptedTokenB64 = Convert.ToBase64String(encrypted);
        Log.LogInformation("2. Wysłanie żądania uwierzytelnienia tokenem KSeF");
        Trace.Assert(!string.IsNullOrEmpty(config.Nip), "--nip jest empty");
        AuthenticationKsefTokenRequest request = new AuthenticationKsefTokenRequest
        {
            Challenge = challenge.Challenge,
            ContextIdentifier = new AuthenticationTokenContextIdentifier
            {
                Type = AuthenticationTokenContextIdentifierType.Nip,
                Value = config.Nip
            },
            EncryptedToken = encryptedTokenB64,
            AuthorizationPolicy = null
        };
        SignatureResponse signature = await ksefClient.SubmitKsefTokenAuthRequestAsync(request, new CancellationToken()).ConfigureAwait(false);
        Log.LogInformation("3. Sprawdzenie statusu uwierzytelniania");
        DateTime startTime = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMinutes(2);
        AuthStatus status;
        do
        {
            status = await ksefClient.GetAuthStatusAsync(signature.ReferenceNumber, signature.AuthenticationToken.Token).ConfigureAwait(false);
            Log.LogInformation($"      Status: {StatusInfoToString(status.Status)} | upłynęło: {DateTime.UtcNow - startTime:mm\\:ss}");
            if (status.Status.Code != 200)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }
        while (status.Status.Code == 100 && (DateTime.UtcNow - startTime) < timeout);
        if (status.Status.Code != 200)
        {
            throw new InvalidOperationException($"Uwierzytelnienie nie powiodło się lub przekroczono czas oczekiwania. {StatusInfoToString(status.Status)}");
        }
        Log.LogInformation("4. Uzyskanie tokena dostępowego (accessToken)");
        AuthenticationOperationStatusResponse tokenResponse = await ksefClient.GetAccessTokenAsync(signature.AuthenticationToken.Token).ConfigureAwait(false);
        return tokenResponse;
    }

    public static async Task<AuthenticationOperationStatusResponse> CertAuth(
        ProfileConfig config,
        IServiceScope scope,
        Func<IServiceScope, CancellationToken, Task<ICryptographyService>> GetCryptographicService,
        CancellationToken cancellationToken)
    {
        if (config.AuthMethod != AuthMethod.Xades)
        {
            throw new InvalidOperationException("This command requires certificate authentication.");
        }

        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        ICryptographyService cryptoService = await GetCryptographicService(scope, cancellationToken).ConfigureAwait(false);

        byte[] certBytes = Encoding.UTF8.GetBytes(config.Certificate!.Certificate!);
        X509Certificate2 publicCert = certBytes.LoadCertificate();
        X509Certificate2 certificate = publicCert.MergeWithPemKey(config.Certificate.Private_Key!, config.Certificate.Password);

        Log.LogInformation("[2] Pobieranie wyzwania (challenge) z KSeF...");
        AuthenticationChallengeResponse challengeResponse = await ksefClient.GetAuthChallengeAsync().ConfigureAwait(false);
        Log.LogInformation($"    Challenge: {challengeResponse.Challenge}");
        Log.LogInformation("[3] Budowanie AuthTokenRequest (builder)...");
        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challengeResponse.Challenge)
            .WithContext(AuthenticationTokenContextIdentifierType.Nip, config.Nip)
            .WithIdentifierType(config.Certificate!.SubjectIdentifierType)
            .Build();
        Log.LogInformation("[4] Serializacja żądania do XML (unsigned)...");
        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);
        PrintXmlToConsole(unsignedXml, "XML przed podpisem");
        Log.LogInformation("[6] Podpisywanie XML (XAdES)...");
        string signedXml = SignatureService.Sign(unsignedXml, certificate);
        PrintXmlToConsole(signedXml, "XML po podpisie (XAdES)");
        Log.LogInformation("[7] Wysyłanie podpisanego XML do KSeF...");
        SignatureResponse submission = await ksefClient.SubmitXadesAuthRequestAsync(signedXml, verifyCertificateChain: false).ConfigureAwait(false);
        Log.LogInformation($"    ReferenceNumber: {submission.ReferenceNumber}");
        Log.LogInformation("[8] Odpytanie o status operacji uwierzytelnienia...");
        DateTime startTime = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMinutes(2);
        AuthStatus status;
        do
        {
            status = await ksefClient.GetAuthStatusAsync(submission.ReferenceNumber, submission.AuthenticationToken.Token).ConfigureAwait(false);
            Log.LogInformation($"      Status: {StatusInfoToString(status.Status)} | upłynęło: {DateTime.UtcNow - startTime:mm\\:ss}");
            if (status.Status.Code != 200)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }
        while (status.Status.Code == 100 && (DateTime.UtcNow - startTime) < timeout);
        if (status.Status.Code != 200)
        {
            throw new InvalidOperationException($"Uwierzytelnienie nie powiodło się lub przekroczono czas oczekiwania. {StatusInfoToString(status.Status)}");
        }
        Log.LogInformation("[9] Pobieranie access token...");
        AuthenticationOperationStatusResponse tokenResponse = await ksefClient.GetAccessTokenAsync(submission.AuthenticationToken.Token).ConfigureAwait(false);
        return tokenResponse;
    }

    public static string StatusInfoToString(StatusInfo statusInfo)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append($"Code: {statusInfo.Code}, Description: {statusInfo.Description}");
        if (statusInfo.Details != null && statusInfo.Details.Any())
        {
            sb.Append($", Details: [{string.Join(", ", statusInfo.Details)}]");
        }
        if (statusInfo.Extensions != null && statusInfo.Extensions.Any())
        {
            sb.Append($", Extensions: {{{string.Join(", ", statusInfo.Extensions.Select(kv => $"{kv.Key}: {kv.Value}"))}}}");
        }
        return sb.ToString();
    }

    public static void PrintXmlToConsole(string xml, string title)
    {
        Log.LogInformation($"----- {title} -----");
        Log.LogInformation(xml);
        Log.LogInformation($"----- KONIEC: {title} -----\n");
    }
}
