using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using KSeF.Client.Clients; // For OnlineSessionClient
using KSeF.Client.Core.Models.Sessions.OnlineSession; // For models related to online session
using KSeF.Client.Core.Models.Authorization; // For AuthenticationTokenContextIdentifierType, SubjectIdentifierType, ContextIdentifierType
using KSeF.Client.Core.Interfaces.Clients; // For IAuthorizationClient
using KSeF.Client.Api.Builders.Auth; // For AuthTokenRequestBuilder
using KSeF.Client.Api.Services; // For SignatureService
using KSeFCli.Config; // For AppConfig
using KSeFCli.Services; // For Token
using KSeFCli.Services.Resources; // For StringResources
using System.IO;
using System;
using System.Security.Cryptography.X509Certificates;
using KSeF.Client.Core.Models; // For SubjectIdentifierType, ContextIdentifierType base classes

namespace KSeFCli.Services
{
    public class InvoiceService
    {
        private readonly OnlineSessionClient _onlineSessionClient;
        private readonly TokenStore _tokenStore;
        private readonly IAuthorizationClient _authorizationClient;
        private readonly AppConfig _appConfig;

        public InvoiceService(OnlineSessionClient onlineSessionClient, TokenStore tokenStore, IAuthorizationClient authorizationClient, AppConfig appConfig)
        {
            _onlineSessionClient = onlineSessionClient;
            _tokenStore = tokenStore;
            _authorizationClient = authorizationClient;
            _appConfig = appConfig;
        }

        public async Task<string?> UploadInvoiceAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_appConfig.KsefApi.CertificatePath) || !File.Exists(_appConfig.KsefApi.CertificatePath))
            {
                AnsiConsole.MarkupLine("[red]Error: KsefApi:CertificatePath is not configured or file does not exist.[/]");
                return null;
            }

            if (string.IsNullOrEmpty(_appConfig.KsefApi.Nip))
            {
                AnsiConsole.MarkupLine("[red]Error: KsefApi:Nip is not configured.[/]");
                return null;
            }

            X509Certificate2 clientCertificate;
            try
            {
#pragma warning disable SYSLIB0057 // Type or member is obsolete
                clientCertificate = new X509Certificate2(_appConfig.KsefApi.CertificatePath, _appConfig.KsefApi.CertificatePassword, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
#pragma warning restore SYSLIB0057 // Type or member is obsolete
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine(string.Format(StringResources.CertificateLoadError, ex.Message));
                return null;
            }

            string? sessionAccessToken = null;
            string? sessionReferenceNumber = null;

            try
            {
                AnsiConsole.MarkupLine("[yellow]Opening KSeF online session...[/]");
                var challengeResponse = await _authorizationClient.GetAuthChallengeAsync(cancellationToken);

                var authTokenRequest = AuthTokenRequestBuilder
                    .Create()
                    .WithChallenge(challengeResponse.Challenge)
                    .WithContext(AuthenticationTokenContextIdentifierType.Nip, _appConfig.KsefApi.Nip)
                    .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
                    .Build();

                var unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(authTokenRequest);
                var signedXml = SignatureService.Sign(unsignedXml, clientCertificate);
                var sessionTokenResponse = await _authorizationClient.SubmitXadesAuthRequestAsync(signedXml, false, cancellationToken);

                if (sessionTokenResponse?.AuthenticationToken?.Token == null)
                {
                    AnsiConsole.MarkupLine("[red]Error: Failed to obtain KSeF session token.[/]");
                    return null;
                }

                // Correct OpenOnlineSessionRequest construction and property access
                var openSessionRequest = new OpenOnlineSessionRequest
                {
                    Type = OnlineSessionType.Interactive, // Assuming interactive for now
                    Challenge = challengeResponse.Challenge,
                    Subject = new SubjectIdentifierType
                    {
                        Type = AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject,
                        Identifier = _appConfig.KsefApi.Nip
                    },
                    Context = new ContextIdentifierType
                    {
                        Type = AuthenticationTokenContextIdentifierType.Nip,
                        Identifier = _appConfig.KsefApi.Nip
                    }
                };
                
                var openSessionResponse = await _onlineSessionClient.OpenOnlineSessionAsync(openSessionRequest, sessionTokenResponse.AuthenticationToken.Token, null, cancellationToken);
                sessionAccessToken = openSessionResponse?.SessionToken?.Token;
                sessionReferenceNumber = openSessionResponse?.ReferenceNumber;

                if (string.IsNullOrEmpty(sessionAccessToken) || string.IsNullOrEmpty(sessionReferenceNumber))
                {
                    AnsiConsole.MarkupLine("[red]Error: Failed to open KSeF online session.[/]");
                    return null;
                }

                AnsiConsole.MarkupLine($"[green]KSeF online session opened. Session Reference Number: {sessionReferenceNumber}[/]");

                if (!File.Exists(filePath))
                {
                    AnsiConsole.MarkupLine($"[red]Error: Invoice file not found at '{filePath}'.[/]");
                    return null;
                }

                string invoiceContent;
                try
                {
                    invoiceContent = await File.ReadAllTextAsync(filePath, cancellationToken);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error reading invoice file '{filePath}': {ex.Message}[/]");
                    return null;
                }

                AnsiConsole.MarkupLine($"[yellow]Uploading invoice '{filePath}'...[/]");
                var sendInvoiceRequest = new SendInvoiceRequest // Correct type
                {
                    InvoiceHash = null, // KSeF API will calculate hash
                    InvoicePayload = invoiceContent
                };

                var sendInvoiceResponse = await _onlineSessionClient.SendOnlineSessionInvoiceAsync(sendInvoiceRequest, sessionReferenceNumber, sessionAccessToken, cancellationToken);

                if (sendInvoiceResponse == null || string.IsNullOrEmpty(sendInvoiceResponse.ReferenceNumber))
                {
                    AnsiConsole.MarkupLine($"[red]Error uploading invoice '{filePath}': KSeF API returned no reference number.[/]");
                    return null;
                }

                AnsiConsole.MarkupLine($"[green]Invoice '{filePath}' uploaded successfully. KSeF Reference Number: {sendInvoiceResponse.ReferenceNumber}[/]");
                return sendInvoiceResponse.ReferenceNumber;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error during invoice upload session: {ex.Message}[/]");
                return null;
            }
            finally
            {
                if (!string.IsNullOrEmpty(sessionReferenceNumber) && !string.IsNullOrEmpty(sessionAccessToken))
                {
                    try
                    {
                        AnsiConsole.MarkupLine("[yellow]Closing KSeF online session...[/]");
                        await _onlineSessionClient.CloseOnlineSessionAsync(sessionReferenceNumber, sessionAccessToken, cancellationToken);
                        AnsiConsole.MarkupLine("[green]KSeF online session closed.[/]");
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error closing KSeF online session: {ex.Message}[/]");
                    }
                }
            }
        }
    }
}
