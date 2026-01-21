using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Api.Services;
using KSeF.Client.ClientFactory;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace KSeFCli;

public class CertAuthCommand : BaseKsefCommand<CertAuthCommand.Settings>
{
    private readonly ILogger<CertAuthCommand> _logger;

    public CertAuthCommand(ILogger<CertAuthCommand> logger, IKSeFClientFactory ksefClientFactory)
        : base(ksefClientFactory)
    {
        _logger = logger;
    }

    public class Settings : GlobalSettings
    {
        [CommandOption("--subject-identifier-type")]
        [Description("Type of subject identifier (e.g., CertificateSubject, CertificateFingerprint)")]
        public AuthenticationTokenSubjectIdentifierTypeEnum SubjectIdentifierType { get; set; }
    }

    public override async Task<int> ExecuteWithProfileAsync(CommandContext context, Settings settings, ProfileConfig profile, IKSeFClient client, CancellationToken cancellationToken)
    {
        if (profile.AuthMethod != AuthMethod.Xades)
        {
            _logger.LogError("Active profile is not configured for XAdES authentication.");
            return 1;
        }

        string certificatePath = profile.Certificate!.Certificate;
        string privateKeyPath = profile.Certificate.Private_Key;
        string certificatePassword = System.Environment.GetEnvironmentVariable(profile.Certificate.Password_Env)!;

        X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(privateKeyPath, certificatePassword);

        _logger.LogInformation("1. Getting challenge");
        AuthenticationChallengeResponse challengeResponse = await client.GetAuthChallengeAsync().ConfigureAwait(false);
        _logger.LogInformation($"    Challenge: {challengeResponse.Challenge}");

        _logger.LogInformation("2. Prepare and Sign AuthTokenRequest");
        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
            .Create()
            .WithChallenge(challengeResponse.Challenge)
            .WithContext(AuthenticationTokenContextIdentifierType.Nip, profile.Nip)
            .WithIdentifierType(settings.SubjectIdentifierType)
            .Build();

        _logger.LogInformation("3. Serializacja żądania do XML (unsigned)");
        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);
        _logger.LogDebug($"XML przed podpisem:\n{unsignedXml}");

        _logger.LogInformation("4. Podpisywanie XML (XAdES)");
        string signedXml = SignatureService.Sign(unsignedXml, certificate);
        _logger.LogDebug($"XML po podpisie (XAdES):\n{signedXml}");

        _logger.LogInformation("5. Wysyłanie podpisanego XML do KSeF");
        SignatureResponse submission = await client.SubmitXadesAuthRequestAsync(signedXml, verifyCertificateChain: false).ConfigureAwait(false);
        _logger.LogInformation($"    ReferenceNumber: {submission.ReferenceNumber}");

        _logger.LogInformation("6. Odpytanie o status operacji uwierzytelnienia");
        DateTime startTime = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromMinutes(2);
        AuthStatus status;
        do
        {
            status = await client.GetAuthStatusAsync(submission.ReferenceNumber, submission.AuthenticationToken.Token).ConfigureAwait(false);
            _logger.LogInformation($"      Status: {status.Status.Code} - {status.Status.Description} | upłynęło: {DateTime.UtcNow - startTime:mm:ss}");
            if (status.Status.Code != 200)
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
        }
        while (status.Status.Code == 100 && (DateTime.UtcNow - startTime) < timeout);

        if (status.Status.Code != 200)
        {
            _logger.LogError($"Uwierzytelnienie nie powiodło się lub przekroczono czas oczekiwania. Kod: {status.Status.Code}, Opis: {status.Status.Description}");
            return 1;
        }

        _logger.LogInformation("7. Pobieranie access token");
        AuthenticationOperationStatusResponse tokenResponse = await client.GetAccessTokenAsync(submission.AuthenticationToken.Token).ConfigureAwait(false);

        Console.Out.WriteLine(JsonSerializer.Serialize(tokenResponse));
        _logger.LogInformation("Zakończono pomyślnie.");

        return 0;
    }
}
