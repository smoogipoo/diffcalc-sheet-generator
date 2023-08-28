using Generator;
using Generator.Diff;
using Generator.Generators;

if (Env.GENERATOR_LIST.Length == 0)
    Console.WriteLine("Nothing to generate (no generators)!");

DiffSpreadSheet spreadsheet = await DiffSpreadSheet.Create(
    $"{Env.RULESET}: "
    + $"{Env.DB_A[..7]} (A) vs {Env.DB_B[..7]} (B) "
    + "| "
    + $"converts: {!Env.NO_CONVERTS}, "
    + $"ranked-only: {Env.RANKED_ONLY}");

Console.WriteLine($"Spreadsheet created: {spreadsheet.SpreadSheet.SpreadsheetUrl}");
Console.WriteLine($"Now generating (generators: {string.Join(", ", Env.GENERATOR_LIST)})");

List<IGenerator> generators = new List<IGenerator>();

foreach (string gen in Env.GENERATOR_LIST)
{
    switch (gen)
    {
        case "pp":
            generators.Add(new PerformanceDiffsGenerator(true, Order.Gains));
            generators.Add(new PerformanceDiffsGenerator(true, Order.Losses));
            generators.Add(new PerformanceDiffsGenerator(false, Order.Gains));
            generators.Add(new PerformanceDiffsGenerator(false, Order.Losses));
            break;

        case "sr":
            generators.Add(new StarRatingDiffsGenerator(true, Order.Gains));
            generators.Add(new StarRatingDiffsGenerator(true, Order.Losses));
            generators.Add(new StarRatingDiffsGenerator(false, Order.Gains));
            generators.Add(new StarRatingDiffsGenerator(false, Order.Losses));
            break;
    }
}

List<Task<object[][]>> rows = generators.Select(gen => gen.Query()).ToList();
await Task.WhenAll(rows);

for (int i = 0; i < generators.Count; i++)
    await spreadsheet.AddSheet(generators[i], rows[i].Result);

Console.WriteLine($"Spreadsheet generation finished: {spreadsheet.SpreadSheet.SpreadsheetUrl}");
