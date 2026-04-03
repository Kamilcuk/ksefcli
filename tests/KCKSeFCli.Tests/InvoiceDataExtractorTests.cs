using KCKSeFCli;

namespace KCKSeFCli.Tests;

public class InvoiceDataExtractorTests {
    [Fact]
    public void Extract_ReturnsCorrectData() {
        string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://crd.gov.pl/wzor/2025/06/25/13775/"">
    <Fa>
        <P_13_1>1000.00</P_13_1>
        <P_13_2>200.00</P_13_2>
        <P_15>1476.00</P_15>
        <FaWiersz>
            <P_7>Product 1</P_7>
            <P_8B>2</P_8B>
            <P_11>1000.00</P_11>
        </FaWiersz>
        <FaWiersz>
            <P_7>Product 2</P_7>
            <P_8B>1</P_8B>
            <P_11>200.00</P_11>
        </FaWiersz>
    </Fa>
</Faktura>";

        var data = InvoiceDataExtractor.Extract(xml);

        Assert.Equal(1476.00m, data.TotalBrutto);
        Assert.Equal(1200.00m, data.TotalNetto);
        Assert.Equal(2, data.Items.Count);
        Assert.Equal("Product 1", data.Items[0].Name);
        Assert.Equal(2m, data.Items[0].Quantity);
        Assert.Equal(1000.00m, data.Items[0].NetValue);
    }

    [Fact]
    public void Extract_WorksWithDifferentNamespace() {
        string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Faktura xmlns=""http://crd.gov.pl/wzor/2021/11/29/11073/"">
    <Fa>
        <P_15>100.00</P_15>
    </Fa>
</Faktura>";

        var data = InvoiceDataExtractor.Extract(xml);

        Assert.Equal(100.00m, data.TotalBrutto);
    }
}
