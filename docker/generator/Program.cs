using Generator.Diff;

string osuA = Environment.GetEnvironmentVariable("OSU_A_HASH") ?? throw new InvalidOperationException("Missing OSU_A_HASH environment variable.");
string osuB = Environment.GetEnvironmentVariable("OSU_B_HASH") ?? throw new InvalidOperationException("Missing OSU_B_HASH environment variable.");

DiffSpreadSheet spreadsheet = await DiffSpreadSheet.Create($"{osuA[..7]} (A) vs {osuB[..7]} (B)");

List<Task> tasks = new List<Task>
{
    new DiffGenerator(osuA, osuB, true, DiffType.Gains)
        .QueryPpDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.PpGainsAllSheet, DiffSpreadSheet.MakePpGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(osuA, osuB, true, DiffType.Losses)
        .QueryPpDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.PpLossesAllSheet, DiffSpreadSheet.MakePpGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(osuA, osuB, false, DiffType.Gains)
        .QueryPpDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.PpGainsNMSheet, DiffSpreadSheet.MakePpGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(osuA, osuB, false, DiffType.Losses)
        .QueryPpDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.PpLossesNMSheet, DiffSpreadSheet.MakePpGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(osuA, osuB, true, DiffType.Gains)
        .QuerySrDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.SrGainsAllSheet, DiffSpreadSheet.MakeSrGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(osuA, osuB, true, DiffType.Losses)
        .QuerySrDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.SrLossesAllSheet, DiffSpreadSheet.MakeSrGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(osuA, osuB, false, DiffType.Gains)
        .QuerySrDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.SrGainsNMSheet, DiffSpreadSheet.MakeSrGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap(),
    new DiffGenerator(osuA, osuB, false, DiffType.Losses)
        .QuerySrDiffs()
        .ContinueWith(
            async t => await spreadsheet.SetData(spreadsheet.SrLossesNMSheet, DiffSpreadSheet.MakeSrGrid(t.Result)),
            TaskContinuationOptions.OnlyOnRanToCompletion)
        .Unwrap()
};

await Task.WhenAll(tasks);

Console.WriteLine($"Spreadsheet generated. {spreadsheet.SpreadSheet.SpreadsheetUrl}");