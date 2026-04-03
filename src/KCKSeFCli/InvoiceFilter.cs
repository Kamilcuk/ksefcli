using KSeF.Client.Core.Models.Invoices;

namespace KCKSeFCli;

public static class InvoiceFilter {
    public static List<InvoiceSummary> FilterNewInvoices(
        List<InvoiceSummary> invoices,
        DateTimeOffset? lastInvoicingDate,
        string? lastKsefNumber) {
        
        return invoices
            .Where(i => !lastInvoicingDate.HasValue || i.InvoicingDate > lastInvoicingDate.Value || 
                        (i.InvoicingDate == lastInvoicingDate.Value && string.Compare(i.KsefNumber, lastKsefNumber) > 0))
            .OrderBy(i => i.InvoicingDate)
            .ThenBy(i => i.KsefNumber)
            .ToList();
    }
}
