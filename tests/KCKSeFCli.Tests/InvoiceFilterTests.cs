using KSeF.Client.Core.Models.Invoices;
using KCKSeFCli;

namespace KCKSeFCli.Tests;

public class InvoiceFilterTests {
    [Fact]
    public void FilterNewInvoices_OnFirstRun_ReturnsAllSorted() {
        var invoices = new List<InvoiceSummary> {
            new InvoiceSummary { KsefNumber = "B", InvoicingDate = DateTimeOffset.Parse("2023-01-01T10:00:00Z") },
            new InvoiceSummary { KsefNumber = "A", InvoicingDate = DateTimeOffset.Parse("2023-01-01T09:00:00Z") },
        };

        var result = InvoiceFilter.FilterNewInvoices(invoices, null, null);

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].KsefNumber);
        Assert.Equal("B", result[1].KsefNumber);
    }

    [Fact]
    public void FilterNewInvoices_WithState_FiltersCorrectly() {
        var lastDate = DateTimeOffset.Parse("2023-01-01T10:00:00Z");
        var invoices = new List<InvoiceSummary> {
            new InvoiceSummary { KsefNumber = "A", InvoicingDate = DateTimeOffset.Parse("2023-01-01T09:00:00Z") },
            new InvoiceSummary { KsefNumber = "B", InvoicingDate = lastDate },
            new InvoiceSummary { KsefNumber = "C", InvoicingDate = lastDate },
            new InvoiceSummary { KsefNumber = "D", InvoicingDate = DateTimeOffset.Parse("2023-01-01T11:00:00Z") },
        };

        // Last processed was B. C is > B (alphabetically), so it should stay.
        var result = InvoiceFilter.FilterNewInvoices(invoices, lastDate, "B");

        Assert.Equal(2, result.Count);
        Assert.Equal("C", result[0].KsefNumber);
        Assert.Equal("D", result[1].KsefNumber);
    }

    [Fact]
    public void FilterNewInvoices_WithSmallerKsefNumber_FiltersItOut() {
        var lastDate = DateTimeOffset.Parse("2023-01-01T10:00:00Z");
        var invoices = new List<InvoiceSummary> {
            new InvoiceSummary { KsefNumber = "A", InvoicingDate = lastDate },
            new InvoiceSummary { KsefNumber = "B", InvoicingDate = lastDate },
        };

        // If B was processed, A (which is < B) should be filtered out
        var result = InvoiceFilter.FilterNewInvoices(invoices, lastDate, "B");

        Assert.Empty(result);
    }

    [Fact]
    public void FilterNewInvoices_WithNewerTimestamp_ReturnsAllNewer() {
        var lastDate = DateTimeOffset.Parse("2023-01-01T10:00:00Z");
        var invoices = new List<InvoiceSummary> {
            new InvoiceSummary { KsefNumber = "B", InvoicingDate = lastDate },
            new InvoiceSummary { KsefNumber = "D", InvoicingDate = DateTimeOffset.Parse("2023-01-01T11:00:00Z") },
        };

        // If we just got newer ones, but LastDate is still 10:00:00, it should return them
        var result = InvoiceFilter.FilterNewInvoices(invoices, lastDate, "B");

        Assert.Single(result);
        Assert.Equal("D", result[0].KsefNumber);
    }
}
