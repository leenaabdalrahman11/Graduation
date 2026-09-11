using Microsoft.EntityFrameworkCore;
using MyApi.DAL.Data;
using MyApi.DAL.Models;
using System.Globalization;
using System.Text.RegularExpressions;
namespace MyApi.DAL.Data.Seeders;

public static class ProductSeeder
{
    public static async Task SeedProducts(ApplicationDbContext context)
    {
        if (await context.Products.AnyAsync())
            return;


var csvPath = Environment.GetEnvironmentVariable("PRODUCTS_CSV_PATH");

if (string.IsNullOrWhiteSpace(csvPath) || !File.Exists(csvPath))
{
    Console.WriteLine(
        "Products CSV was not provided. Product seeding was skipped."
    );

    return;
}

List<string> lines = new();

using (var stream = new FileStream(
    csvPath,
    FileMode.Open,
    FileAccess.Read,
    FileShare.ReadWrite))
{
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream)
    {
        lines.Add(await reader.ReadLineAsync());
    }
}

foreach (var line in lines.Skip(1))
{
    if (string.IsNullOrWhiteSpace(line))
        continue;

var columns = Regex.Split(
    line,
    ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"
);
    if (columns.Length < 7)
    {
        Console.WriteLine($"Invalid row: {line}");
        continue;
    }

    var name = columns[1].Trim('"');
    var description = columns[2].Trim('"');
    var categoryName = columns[3].Trim('"');

    var price = decimal.Parse(
        columns[4].Trim('"'),
        CultureInfo.InvariantCulture
    );

    var stock = int.Parse(columns[5].Trim('"'));

    var image = columns[6].Trim('"');


            var categoryId = await context.CategoryTranslations
                .Where(x => x.Name == categoryName && x.Language == "en")
                .Select(x => x.CategoryId)
                .FirstOrDefaultAsync();


            if(categoryId == 0)
            {
                Console.WriteLine(
                    $"Category not found: {categoryName}"
                );
                continue;
            }



            var product = new Product
            {
                Price = price,
                Stock = stock,
                Quantity = stock,
                Rate = 0,
                Discount = 0,
                Status = Status.Active,
                MainImage = image,
                CategoryId = categoryId
            };


            context.Products.Add(product);

            await context.SaveChangesAsync();


            var translation = new ProductTranslation
            {
                ProductId = product.Id,
                Language = "en",
                Name = name,
                Description = description
            };


            context.ProductTranslations.Add(translation);

            await context.SaveChangesAsync();
        }


        Console.WriteLine("Products imported successfully");
    }
}