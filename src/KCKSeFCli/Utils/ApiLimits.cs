// Copied from thirdparty/ksef-client-csharp/KSeF.Client.Tests.Core/Utils/RateLimit/ApiLimits.cs
namespace KCKSeFCli.Utils;

/// <summary>
/// Reprezentuje limity konkretnego endpointu API KSeF.
/// </summary>
public record ApiLimits
{
    public int RequestsPerSecond { get; init; }
    
    public int RequestsPerMinute { get; init; }
    
    public int RequestsPerHour { get; init; }
}
