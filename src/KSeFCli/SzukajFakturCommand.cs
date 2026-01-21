using CommandLine;
using KSeF.Client.Clients;
using KSeF.Client.Core.Models.Invoices;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace KSeFCli;

[Verb("SzukajFaktur", HelpText = "Query invoice metadata")]
public class SzukajFakturCommand : GlobalCommand
{
    [Option('s', "subject-type", Required = true, HelpText = "Subject type (Subject1, Subject2, etc.)")]
    public string SubjectType { get; set; }

    [Option("from", Required = true, HelpText = "Start date in ISO-8601 format")]
    public DateTime From { get; set; }

    [Option("to", Required = true, HelpText = "End date in ISO-8601 format")]
    public DateTime To { get; set; }

    [Option("date-type", Default = "Issue", HelpText = "Date type (Issue, Invoicing, PermanentStorage)")]
    public string DateType { get; set; }

    [Option("page-offset", Default = 0, HelpText = "Page offset for pagination")]
    public int PageOffset { get; set; }

    [Option("page-size", Default = 10, HelpText = "Page size for pagination")]
    public int PageSize { get; set; }

    public override async Task<int> ExecuteAsync(CancellationToken cancellationToken)

    {

        var serviceProvider = GetServiceProvider();

        var ksefClient = serviceProvider.GetRequiredService<KSeFClient>();





        if (!Enum.TryParse(SubjectType, true, out InvoiceSubjectType subjectType))

        {

            Console.Error.WriteLine($"Invalid SubjectType: {SubjectType}");

            return 1;

        }



        if (!Enum.TryParse(DateType, true, out DateType dateType))

        {

            Console.Error.WriteLine($"Invalid DateType: {DateType}");

            return 1;

        }



        var queryFilters = new InvoiceQueryFilters

        {

            SubjectType = subjectType,

            DateRange = new DateRange

            {

                From = From,

                To = To,

                DateType = dateType

            }

        };



        var response = await ksefClient.QueryInvoiceMetadataAsync(queryFilters, Token, PageOffset, PageSize, KSeF.Client.Core.Models.Invoices.SortOrder.Asc, cancellationToken);

        Console.WriteLine(JsonSerializer.Serialize(response));

        return 0;

    }
}
