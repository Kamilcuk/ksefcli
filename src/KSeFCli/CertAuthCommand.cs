using CommandLine;
using KSeF.Client.Clients;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using KSeF.Client.Api.Services;

namespace KSeFCli;

[Verb("CertAuth", HelpText = "Authenticate using a certificate")]
public class CertAuthCommand : GlobalCommand
{
    [Option("certificate-path", Required = true, HelpText = "Path to the certificate file (.pfx)")]
    public required string CertificatePath { get; set; }

    [Option("certificate-password", HelpText = "Password for the certificate file")]
    public required string CertificatePassword { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)
    {
        var serviceProvider = GetServiceProvider();
        var ksefClient = serviceProvider.GetRequiredService<KSeFClient>();
        var cryptoService = serviceProvider.GetRequiredService<ICryptographyService>();
        var signatureService = serviceProvider.GetRequiredService<SignatureService>();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();



        logger.LogInformation("1. Uzyskanie auth challenge");
        AuthenticationChallengeResponse challenge = await ksefClient.GetAuthChallengeAsync(cancellationToken).ConfigureAwait(false);

        X509Certificate2 certificate = X509CertificateLoader.LoadPkcs12FromFile(CertificatePath, CertificatePassword);

        logger.LogInformation("1. Przygotowanie dokumentu XML (AuthTokenRequest)");
        var signedChallenge = signatureService.Sign(challenge.Challenge, certificate);

        logger.LogInformation("3. Submitting certificate authorization request");
        AuthenticationCertificateRequest request = new AuthenticationCertificateRequest
        {
            Challenge = challenge.Challenge,
            ContextIdentifier = new AuthenticationCertificateContextIdentifier
            {
                Type = AuthenticationCertificateContextIdentifierType.Nip,
                Value = Nip
            },
            Signature = signedChallenge,
            AuthorizationPolicy = null
        };

        SignatureResponse signature = await ksefClient.SubmitCertificateAuthRequestAsync(request, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("4. Checking authentication status");
        AuthStatus status = await ksefClient.GetAuthStatusAsync(signature.ReferenceNumber, signature.AuthenticationToken.Token, cancellationToken).ConfigureAwait(false);

        if (status.Status.Code != 200)
        {
            logger.LogError($"Certificate authentication failed. Code: {status.Status.Code}, Description: {status.Status.Description}");
            return 1;
        }

        logger.LogInformation("5. Getting access token");
        AuthenticationOperationStatusResponse tokenResponse = await ksefClient.GetAccessTokenAsync(signature.AuthenticationToken.Token, cancellationToken).ConfigureAwait(false);

        Console.Out.WriteLine(JsonSerializer.Serialize(tokenResponse));
        logger.LogInformation("Certificate authentication completed successfully.");
        return 0;
    }
}
