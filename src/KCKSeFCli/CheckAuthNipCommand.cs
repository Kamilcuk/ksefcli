using CommandLine;
using Microsoft.Extensions.DependencyInjection;
namespace KCKSeFCli;
[Verb("CheckAuthNip", HelpText = "Check if NIP from authentication (token or certificate) matches NIP in configuration")]
public class CheckAuthNipCommand : IWithConfigCommand {
    public override Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        ProfileConfigWithName config = Config();
        string? authNip = null;
        if (config.AuthMethod == AuthMethod.KsefToken) {
            authNip = NipUtils.ExtractNipFromToken(config.Token!);
            Log.Information($"NIP extracted from token: {authNip}");
        } else if (config.AuthMethod == AuthMethod.Xades) {
            authNip = NipUtils.GetNipFromCertificate(config.Certificate?.Certificate);
            if (authNip == null) {
                string? subject = NipUtils.GetCertificateSubject(config.Certificate?.Certificate);
                throw new InvalidOperationException($"Could not extract NIP from certificate. Certificate Subject: {subject}");
            }
            Log.Information($"NIP extracted from certificate: {authNip}");
        }
        if (string.IsNullOrEmpty(config.Nip)) {
             Log.Information("NIP not specified in configuration, using NIP from authentication.");
        } else {
            if (!NipUtils.MatchNip(authNip, config.Nip)) {
                throw new InvalidOperationException($"NIP mismatch! Auth NIP: {authNip}, Config NIP: {config.Nip}");
            }
            Log.Information("NIP matches configuration.");
        }
        return Task.FromResult(0);
    }
}
