using System.Text;
using Generator;
using Generator.Diff;
using Generator.Generators;

if (Env.GENERATOR_LIST.Length == 0)
    Console.WriteLine("Nothing to generate (no generators)!");

StringBuilder titleBuilder = new StringBuilder();
titleBuilder.Append($"{Env.RULESET}: ");
titleBuilder.Append($"{Env.DB_A[..7]} (A) vs {Env.DB_B[..7]} (B) ");
titleBuilder.Append("| ");
titleBuilder.Append($"converts: {!Env.NO_CONVERTS}, ");
titleBuilder.Append($"aspire: {!Env.NO_ASPIRE}, ");
titleBuilder.Append($"ranked-only: {Env.RANKED_ONLY}");
if (Env.MOD_FILTERS.Length != 0)
    titleBuilder.Append($", mods: {Env.MOD_FILTERS_RAW}");

DiffSpreadSheet spreadsheet = await DiffSpreadSheet.Create(titleBuilder.ToString());

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

            if (Env.MOD_FILTERS.Length == 0)
            {
                generators.Add(new PerformanceDiffsGenerator(false, Order.Gains));
                generators.Add(new PerformanceDiffsGenerator(false, Order.Losses));
            }

            break;

        case "sr":
            generators.Add(new StarRatingDiffsGenerator(true, Order.Gains));
            generators.Add(new StarRatingDiffsGenerator(true, Order.Losses));

            if (Env.MOD_FILTERS.Length == 0)
            {
                generators.Add(new StarRatingDiffsGenerator(false, Order.Gains));
                generators.Add(new StarRatingDiffsGenerator(false, Order.Losses));
            }

            break;

        case "score":
            generators.Add(new ScoreDiffsGenerator(true, Order.Gains));
            generators.Add(new ScoreDiffsGenerator(true, Order.Losses));

            if (Env.MOD_FILTERS.Length == 0)
            {
                generators.Add(new ScoreDiffsGenerator(false, Order.Gains));
                generators.Add(new ScoreDiffsGenerator(false, Order.Losses));
            }

            break;
    }
}

List<Task<object[][]>> rows = generators.Select(gen => gen.Query()).ToList();
await Task.WhenAll(rows);

for (int i = 0; i < generators.Count; i++)
    await spreadsheet.AddSheet(generators[i], rows[i].Result);

await spreadsheet.Close();

Console.WriteLine($"Spreadsheet generation finished: {spreadsheet.SpreadSheet.SpreadsheetUrl}");
