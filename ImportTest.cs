// Quick test to verify CSV import works
using CharacterManager.Data;
using CharacterManager.Services;
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("Data Source=CharacterManager/charactermanager.db")
    .Options;

using var context = new AppDbContext(options);
var service = new PersonnageService(context);
var csvImporter = new CsvImportService(service);

// Read CSV file
using var fs = File.OpenRead("personnages_lust_goddess.csv");
var result = await csvImporter.ImportCsvAsync(fs);

Console.WriteLine($"Import Success: {result.IsSuccess}");
Console.WriteLine($"Personnages imported: {result.SuccessCount}");
if (!string.IsNullOrEmpty(result.Error))
    Console.WriteLine($"Error: {result.Error}");
if (result.Errors.Count > 0)
{
    Console.WriteLine("Errors encountered:");
    foreach (var err in result.Errors.Take(5))
        Console.WriteLine($"  - {err}");
}

// Check total count
var count = context.Personnages.Count();
Console.WriteLine($"Total personnages in database: {count}");
