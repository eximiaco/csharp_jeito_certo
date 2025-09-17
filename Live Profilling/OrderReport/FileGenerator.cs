using Bogus;

namespace OrderReport;

public static class FileGenerator
{
    public static async Task GenerateFakeData()
    {
        if (!File.Exists("sales_report_big.csv"))
        {
            var salesBig = GenerateFakeSalesData(5_000_000);
            await File.WriteAllLinesAsync("sales_report_big.csv", salesBig.Select(record => record.ToCsv()));
        }

        if (!File.Exists("sales_report_small.csv"))
        {
            var salesSmall = GenerateFakeSalesData(1_000);
            await File.WriteAllLinesAsync("sales_report_small.csv", salesSmall.Select(record => record.ToCsv()));
        }
    }

    private static IEnumerable<SaleRecord> GenerateFakeSalesData(int count)
    {
        var faker = new Faker<SaleRecord>()
            .RuleFor(s => s.Id, f => Guid.NewGuid())
            .RuleFor(s => s.ProductName, f => f.Commerce.ProductName())
            .RuleFor(s => s.ProductCategory, f => f.PickRandom("Electronics", "Books", "Clothing", "Toys"))
            .RuleFor(s => s.Amount, f => f.Random.Double(5.0, 500.0))
            .RuleFor(s => s.Timestamp, f => f.Date.Recent());

        return faker.Generate(count);
    }

    class SaleRecord
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; }
        public string ProductCategory { get; set; }
        public double Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string ToCsv() => $"{Id},{ProductName},{ProductCategory},{Amount:F2},{Timestamp:O}";
    }
}
