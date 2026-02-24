using System.Text.Json;

using CommandLine;

using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.TestData;

using Microsoft.Extensions.DependencyInjection;

namespace KCKSeFCli;

[Verb("PokazLimity", HelpText = "Show limits for the current context, subject and attachment permission status.")]
public class PokazLimityCommand : IWithConfigCommand {
    public override async Task<int> ExecuteInScopeAsync(IServiceScope scope, CancellationToken cancellationToken) {
        IKSeFClient ksefClient = scope.ServiceProvider.GetRequiredService<IKSeFClient>();
        ILimitsClient limitsClient = scope.ServiceProvider.GetRequiredService<ILimitsClient>();
        string accessToken = await GetAccessToken(scope, cancellationToken).ConfigureAwait(false);

        Log.LogInformation("Pobieranie limitów kontekstu...");
        SessionLimitsInCurrentContextResponse contextLimits = await limitsClient.GetLimitsForCurrentContextAsync(accessToken, cancellationToken).ConfigureAwait(false);

        Log.LogInformation("Pobieranie limitów podmiotu...");
        CertificatesLimitInCurrentSubjectResponse subjectLimits = await limitsClient.GetLimitsForCurrentSubjectAsync(accessToken, cancellationToken).ConfigureAwait(false);

        Log.LogInformation("Pobieranie statusu uprawnień do załączników...");
        PermissionsAttachmentAllowedResponse attachmentStatus = await ksefClient.GetAttachmentPermissionStatusAsync(accessToken, cancellationToken).ConfigureAwait(false);

        var result = new {
            ContextLimits = contextLimits,
            SubjectLimits = subjectLimits,
            AttachmentPermission = attachmentStatus
        };

        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

        return 0;
    }
}
