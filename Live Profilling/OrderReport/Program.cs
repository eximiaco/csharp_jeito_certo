using Microsoft.AspNetCore.Mvc;
using System.Buffers;
using System.Globalization;
using System.Text;
using static OrderReport.StringHelper;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();
var fileName = "sales_report_small.csv";

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/sum-by-category-v1", async ([FromQuery] string category) =>
{
    var file = await File.ReadAllTextAsync(fileName);
    var totalSum = 0m;

    foreach (var line in file.Split('\n'))
    {
        var columns = line.Split(',');
        var productCategory = columns[2];
        var amount = decimal.Parse(columns[3]);

        if (productCategory == category)
        {
            totalSum += amount;
        }
    }

    return Results.Ok(new
    {
        Category = category,
        TotalSum = totalSum.ToString("C", CultureInfo.CurrentCulture)
    });
});

app.MapGet("/sum-by-category-v2", async ([FromQuery] string category) =>
{
    var totalSum = 0m;

    await foreach (var line in File.ReadLinesAsync(fileName))
    {
        var columns = line.Split(',');
        var productCategory = columns[2];
        var amount = decimal.Parse(columns[3]);

        if (productCategory == category)
        {
            totalSum += amount;
        }
    }

    return Results.Ok(new
    {
        Category = category,
        TotalSum = totalSum.ToString("C", CultureInfo.CurrentCulture)
    });
});

app.MapGet("/sum-by-category-v3", async ([FromQuery] string category) =>
{
    var totalSum = 0m;

    await foreach (var line in File.ReadLinesAsync(fileName))
    {
        var productCategory = GetColumnValueAsSpan(line, 2);
        var amount = decimal.Parse(GetColumnValueAsSpan(line, 3));

        if (productCategory.SequenceEqual(category.AsSpan()))
        {
            totalSum += amount;
        }
    }

    return Results.Ok(new
    {
        Category = category,
        TotalSum = totalSum.ToString("C", CultureInfo.CurrentCulture)
    });
});

app.MapGet("/sum-by-category-v4", async ([FromQuery] string category) =>
{
    var totalSum = 0m;

    const int bufferSize = 10_240;
    var buffer = ArrayPool<char>.Shared.Rent(bufferSize);

    using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize);
    using var reader = new StreamReader(stream, Encoding.UTF8);

    try
    {
        int charsRead = 0;
        int start = 0;
        int length = 0;

        while ((charsRead = await reader.ReadAsync(buffer, length, bufferSize - length)) > 0)
        {
            int total = length + charsRead;

            int pointer = 0;

            while (pointer < total)
            {
                if (buffer[pointer] == '\n')
                {
                    var lineLength = (pointer > 0 && buffer[pointer - 1] == '\r') ? pointer - 1 : pointer;
                    var lineSpan = buffer.AsSpan(start, lineLength);

                    var productCategory = GetColumnValueAsSpan(lineSpan, 2);

                    if (productCategory.SequenceEqual(category.AsSpan()))
                    {
                        var amount = decimal.Parse(GetColumnValueAsSpan(lineSpan, 3));
                        totalSum += amount;
                    }

                    int remaining = total - (pointer + 1);
                    buffer.AsSpan(pointer + 1, remaining).CopyTo(buffer);
                    length = remaining;
                    pointer = -1;
                    total = length;
                }

                pointer++;
            }

            length = total;
        }

        if (length > 0)
        {
            var lineSpan = buffer.AsSpan(0, length);
            var productCategory = GetColumnValueAsSpan(lineSpan, 2);
            var amount = decimal.Parse(GetColumnValueAsSpan(lineSpan, 3));

            if (productCategory.SequenceEqual(category.AsSpan()))
            {
                totalSum += amount;
            }
        }
    }
    finally
    {
        ArrayPool<char>.Shared.Return(buffer);
    }

    return Results.Ok(new
    {
        Category = category,
        TotalSum = totalSum.ToString("C", CultureInfo.CurrentCulture)
    });
});

app.Run();