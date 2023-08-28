using Generator;
using Generator.Diff;
using Generator.Generators;

DiffSpreadSheet spreadsheet = await DiffSpreadSheet.Create(
    $"{Env.RULESET}: "
    + $"{Env.DB_A[..7]} (A) vs {Env.DB_B[..7]} (B) "
    + "| "
    + $"converts: {!Env.NO_CONVERTS}, "
    + $"ranked-only: {Env.RANKED_ONLY}");

Console.WriteLine($"Spreadsheet created: {spreadsheet.SpreadSheet.SpreadsheetUrl}");
Console.WriteLine("Now generating...");

List<IGenerator> generators = new List<IGenerator>
{
    new PerformanceDiffsGenerator(true, Order.Gains),
    new PerformanceDiffsGenerator(true, Order.Losses),
    new PerformanceDiffsGenerator(false, Order.Gains),
    new PerformanceDiffsGenerator(false, Order.Losses),
    new StarRatingDiffsGenerator(true, Order.Gains),
    new StarRatingDiffsGenerator(true, Order.Losses),
    new StarRatingDiffsGenerator(false, Order.Gains),
    new StarRatingDiffsGenerator(false, Order.Losses),
};

List<Task<object[][]>> rows = generators.Select(gen => gen.Query()).ToList();
await Task.WhenAll(rows);

for (int i = 0; i < generators.Count; i++)
    await spreadsheet.AddSheet(generators[i], rows[i].Result);

Console.WriteLine($"Spreadsheet generation finished: {spreadsheet.SpreadSheet.SpreadsheetUrl}");
