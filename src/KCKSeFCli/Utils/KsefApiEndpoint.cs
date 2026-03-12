// Copied from thirdparty/ksef-client-csharp/KSeF.Client.Tests.Core/Utils/RateLimit/KsefApiEndpoint.cs
namespace KCKSeFCli.Utils;

/// <summary>
/// Definiuje typy adresów URL - KSeF API
/// </summary>
public enum KsefApiEndpoint
{
    InvoiceQueryMetadata,
    InvoiceExport,
    InvoiceGetByNumber,
    SessionBatchOpen,
    SessionBatchClose,
    SessionOnlineOpen,
    SessionOnlineSendInvoice,
    SessionOnlineClose,
    SessionInvoiceStatus,
    Other
}
