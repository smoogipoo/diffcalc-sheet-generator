using Generator;
using Generator.Diff;

DiffSpreadSheet spreadsheet = await DiffSpreadSheet.Create(
    $"{Env.RULESET}: "
    + $"{Env.DB_A[..7]} (A) vs {Env.DB_B[..7]} (B) "
    + "| "
    + $"converts: {Env.NO_CONVERTS}, "
    + $"ranked-only: {Env.RANKED_ONLY}");

Console.WriteLine($"Spreadsheet created: {spreadsheet.SpreadSheet.SpreadsheetUrl}");
Console.WriteLine("Now generating...");

List<Task> tasks = new List<Task>
{
    new DiffGenerator(true, DiffType.Gains)
        .QueryPpDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.PpGainsAllSheet, DiffSpreadSheet.MakePpGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(true, DiffType.Losses)
        .QueryPpDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.PpLossesAllSheet, DiffSpreadSheet.MakePpGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(false, DiffType.Gains)
        .QueryPpDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.PpGainsNMSheet, DiffSpreadSheet.MakePpGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(false, DiffType.Losses)
        .QueryPpDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.PpLossesNMSheet, DiffSpreadSheet.MakePpGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(true, DiffType.Gains)
        .QuerySrDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.SrGainsAllSheet, DiffSpreadSheet.MakeSrGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(true, DiffType.Losses)
        .QuerySrDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.SrLossesAllSheet, DiffSpreadSheet.MakeSrGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(false, DiffType.Gains)
        .QuerySrDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.SrGainsNMSheet, DiffSpreadSheet.MakeSrGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(false, DiffType.Losses)
        .QuerySrDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.SrLossesNMSheet, DiffSpreadSheet.MakeSrGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap()
};

await Task.WhenAll(tasks);

Console.WriteLine("Spreadsheet generation finished.");
