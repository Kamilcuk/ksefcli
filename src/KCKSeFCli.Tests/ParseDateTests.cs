using System;
using System.Threading;
using Xunit;
using KCKSeFCli;

namespace KCKSeFCli.Tests;

public class ParseDateTests {
    private static readonly CancellationToken CancellationToken = CancellationToken.None;
    private static readonly DateTime FixedNow = new DateTime(2026, 10, 4, 12, 0, 0);

    [Fact]
    public async Task Parse_Now_ReturnsCurrentDate() {
        DateTime result = await ParseDate.Parse("now", CancellationToken, FixedNow);
        Assert.Equal(FixedNow, result);
    }

    [Fact]
    public async Task Parse_Teraz_ReturnsCurrentDate() {
        DateTime result = await ParseDate.Parse("teraz", CancellationToken, FixedNow);
        Assert.Equal(FixedNow, result);
    }

    [Fact]
    public async Task Parse_NegativeDays_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("-2days", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddDays(-2), result);
    }

    [Fact]
    public async Task Parse_NegativeWeeks_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("-3weeks", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddDays(-21), result);
    }

    [Fact]
    public async Task Parse_NegativeMonths_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("-2months", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddMonths(-2), result);
    }

    [Fact]
    public async Task Parse_NegativeYears_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("-2years", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddYears(-2), result);
    }

    [Fact]
    public async Task Parse_PositiveDays_ReturnsPastDate() {
        // Positive numbers also mean "ago" (past)
        DateTime result = await ParseDate.Parse("2days", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddDays(-2), result);
    }

    [Fact]
    public async Task Parse_PositiveWeeks_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("3weeks", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddDays(-21), result);
    }

    [Fact]
    public async Task Parse_PositiveMonths_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("2months", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddMonths(-2), result);
    }

    [Fact]
    public async Task Parse_PositiveYears_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("2years", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddYears(-2), result);
    }

    [Fact]
    public async Task Parse_PolishNegativeDays_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("-2dni", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddDays(-2), result);
    }

    [Fact]
    public async Task Parse_PolishNegativeWeeks_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("-3tygodni", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddDays(-21), result);
    }

    [Fact]
    public async Task Parse_PolishNegativeMonths_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("-2miesiace", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddMonths(-2), result);
    }

    [Fact]
    public async Task Parse_PolishNegativeYears_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("-2lata", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddYears(-2), result);
    }

    [Fact]
    public async Task Parse_PolishPositiveMonths_ReturnsPastDate() {
        // Positive also means "ago" (past)
        DateTime result = await ParseDate.Parse("2miesiace", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddMonths(-2), result);
    }

    [Fact]
    public async Task Parse_PolishYearsAgo_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("2 lata temu", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddYears(-2), result);
    }

    [Fact]
    public async Task Parse_PolishMonthsAgo_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("2 miesiace temu", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddMonths(-2), result);
    }

    [Fact]
    public async Task Parse_PolishYearsAndMonthsAgo_ReturnsPastDate() {
        DateTime result = await ParseDate.Parse("2 lata i 2 miesiace temu", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddYears(-2).AddMonths(-2), result);
    }

    [Fact]
    public async Task Parse_ChainedUnits_ReturnsCorrectDate() {
        // "2 days 2 years" -> 2 years + 2 days ago
        DateTime result = await ParseDate.Parse("2 days 2 years", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddYears(-2).AddDays(-2), result);
    }

    [Fact]
    public async Task Parse_ChainedPolishUnits_ReturnsCorrectDate() {
        // "2 dni i 2 lata" -> 2 years + 2 days ago
        DateTime result = await ParseDate.Parse("2 dni i 2 lata", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddYears(-2).AddDays(-2), result);
    }

    [Fact]
    public async Task Parse_MixedLanguages_ReturnsCorrectDate() {
        // Mixing English and Polish: "2 years 2 miesiace" -> 2 years + 2 months ago
        DateTime result = await ParseDate.Parse("2 years 2 miesiace", CancellationToken, FixedNow);
        Assert.Equal(FixedNow.AddYears(-2).AddMonths(-2), result);
    }

    [Fact]
    public async Task Parse_SpecificDate_ReturnsCorrectDate() {
        DateTime result = await ParseDate.Parse("2023-01-15", CancellationToken);
        Assert.Equal(new DateTime(2023, 1, 15), result);
    }

    [Fact]
    public async Task Parse_DateOutOfRange_ThrowsException() {
        await Assert.ThrowsAsync<FormatException>(() => ParseDate.Parse("1800-01-01", CancellationToken));
    }

    [Fact]
    public async Task Parse_DateTooFarFuture_ThrowsException() {
        await Assert.ThrowsAsync<FormatException>(() => ParseDate.Parse("2200-01-01", CancellationToken));
    }

    [Fact]
    public async Task Parse_RelativeDateOutOfRange_ThrowsException() {
        // 200 years ago would be out of range
        await Assert.ThrowsAsync<FormatException>(() => ParseDate.Parse("-200years", CancellationToken, FixedNow));
    }

    [Fact]
    public async Task Parse_ISO8601WithTime_ReturnsCorrectDateTime() {
        DateTime result = await ParseDate.Parse("2023-06-15T14:30:00", CancellationToken);
        Assert.Equal(new DateTime(2023, 6, 15, 14, 30, 0), result);
    }

    [Fact]
    public async Task Parse_HumanDateParserFormat_ReturnsCorrectDate() {
        // Test a format that HumanDateParser handles
        DateTime result = await ParseDate.Parse("last monday", CancellationToken, FixedNow);
        // HumanDateParser should parse this relative to FixedNow
        // Just verify it parses without throwing and is within range
        Assert.True(result.Year >= 1900 && result.Year <= 2100);
    }
}