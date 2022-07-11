// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Online.API;
using osu.Game.Rulesets.Mods;

namespace Generator.Diff;

public class DiffSpreadSheet
{
    private const string credentials_file = "/credentials.json";
    private const string sheet_mime = "application/vnd.google-apps.spreadsheet";

    public readonly Spreadsheet SpreadSheet;
    public IList<Sheet> Sheets => SpreadSheet.Sheets;
    public Sheet PpGainsAllSheet => Sheets[0];
    public Sheet PpLossesAllSheet => Sheets[1];
    public Sheet PpGainsNMSheet => Sheets[2];
    public Sheet PpLossesNMSheet => Sheets[3];
    public Sheet SrGainsAllSheet => Sheets[4];
    public Sheet SrLossesAllSheet => Sheets[5];
    public Sheet SrGainsNMSheet => Sheets[6];
    public Sheet SrLossesNMSheet => Sheets[7];

    private readonly SheetsService service;

    private DiffSpreadSheet(Spreadsheet spreadSheet, SheetsService service)
    {
        SpreadSheet = spreadSheet;
        this.service = service;
    }

    public async Task SetData(Sheet sheet, object[][] rows)
    {
        Console.WriteLine($"Setting data on {sheet.Properties.Title}...");

        var request = service.Spreadsheets.Values.Update(new ValueRange { Values = rows.Select(r => (IList<object>)r.ToList()).ToList() }, SpreadSheet.SpreadsheetId, sheet.Properties.Title);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        await request.ExecuteAsync();

        Console.WriteLine($"{sheet.Properties.Title} finished!");
    }

    public static async Task<DiffSpreadSheet> Create(string spreadSheetName)
    {
        var credentials = getCredentials();

        var driveService = getDriveService(credentials);
        var sheetService = getSheetService(credentials);

        // Delete any existing spreadsheet.
        var existingFile = await getSpreadSheetFile(driveService, spreadSheetName);
        if (existingFile != null)
            await driveService.Files.Delete(existingFile.Id).ExecuteAsync();

        // Create sheet.
        var spreadSheet = await sheetService.Spreadsheets.Create(new Spreadsheet
        {
            Properties = new SpreadsheetProperties { Title = spreadSheetName },
            Sheets = new List<Sheet>
            {
                createSheet("PP Gains (All)"),
                createSheet("PP Losses (All)"),
                createSheet("PP Gains (NM)"),
                createSheet("PP Losses (NM)"),
                createSheet("SR Gains (All)"),
                createSheet("SR Losses (All)"),
                createSheet("SR Gains (NM)"),
                createSheet("SR Losses (NM)"),
            }
        }).ExecuteAsync();

        // Embolden first row.
        await sheetService.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
        {
            Requests = spreadSheet.Sheets.Select(sheet => new Request
            {
                RepeatCell = new RepeatCellRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheet.Properties.SheetId,
                        EndRowIndex = 1
                    },
                    Fields = "userEnteredFormat/textFormat",
                    Cell = new CellData
                    {
                        UserEnteredFormat = new CellFormat
                        {
                            TextFormat = new TextFormat
                            {
                                Bold = true,
                            }
                        }
                    }
                }
            }).ToList()
        }, spreadSheet.SpreadsheetId).ExecuteAsync();

        // Enlarge relevant columns.
        await sheetService.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
        {
            Requests = spreadSheet.Sheets.Select((sheet, i) => new Request
            {
                UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheet.Properties.SheetId,
                        Dimension = "COLUMNS",
                        StartIndex = isPPSheet(i) ? 3 : 2,
                        EndIndex = isPPSheet(i) ? 4 : 3,
                    },
                    Properties = new DimensionProperties
                    {
                        PixelSize = 720
                    },
                    Fields = "pixelSize"
                }
            }).ToList()
        }, spreadSheet.SpreadsheetId).ExecuteAsync();

        // Format diff% column as percentage.
        await sheetService.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
        {
            Requests = spreadSheet.Sheets.Select((sheet, i) => new Request
            {
                RepeatCell = new RepeatCellRequest
                {
                    Range = new GridRange
                    {
                        SheetId = sheet.Properties.SheetId,
                        StartRowIndex = 0,
                        StartColumnIndex = isPPSheet(i) ? 7 : 6,
                        EndColumnIndex = isPPSheet(i) ? 8 : 7
                    },
                    Fields = "userEnteredFormat/numberFormat",
                    Cell = new CellData
                    {
                        UserEnteredFormat = new CellFormat
                        {
                            NumberFormat = new NumberFormat
                            {
                                Pattern = "0.00%",
                                Type = "NUMBER"
                            }
                        }
                    }
                }
            }).ToList()
        }, spreadSheet.SpreadsheetId).ExecuteAsync();

        // Share file publicly.
        existingFile = (await getSpreadSheetFile(driveService, spreadSheetName))!;
        await driveService.Permissions.Create(new Permission
        {
            Type = "anyone",
            Role = "reader"
        }, existingFile.Id).ExecuteAsync();

        return new DiffSpreadSheet(spreadSheet, sheetService);

        static bool isPPSheet(int i) => i < 4;
    }

    public static object[][] MakePpGrid(IEnumerable<ProcessedScoreDiff> scores)
    {
        return scores.Select(makePpRow)
                     .Prepend(new object[] { "score_id", "beatmap_id", "enabled_mods", "filename", "pp_master", "pp_pr", "diff", "diff%" })
                     .ToArray();

        static object[] makePpRow(ProcessedScoreDiff score) => new object[]
        {
            score.Diff.highscore_id,
            score.Beatmap.beatmap_id,
            getModString(score.Score.ScoreInfo.mods.ToArray()),
            score.Beatmap.filename,
            score.Diff.a_pp,
            score.Diff.b_pp,
            score.Diff.b_pp - score.Diff.a_pp,
            score.Diff.a_pp == 0 ? 1.0f : (score.Diff.b_pp / score.Diff.a_pp - 1)
        };
    }

    public static object[][] MakeSrGrid(IEnumerable<ProcessedBeatmapDiff> beatmaps)
    {
        return beatmaps.Select(makeSrRow)
                       .Prepend(new object[] { "beatmap_id", "mods", "filename", "sr_master", "sr_pr", "diff", "diff%" })
                       .ToArray();

        static object[] makeSrRow(ProcessedBeatmapDiff beatmap) => new object[]
        {
            beatmap.Beatmap.beatmap_id,
            getModString(LegacyRulesetHelper.GetRulesetFromLegacyId(beatmap.Beatmap.playmode).ConvertFromLegacyMods((LegacyMods)beatmap.Diff.mods).ToArray()),
            beatmap.Beatmap.filename,
            beatmap.Diff.a_sr,
            beatmap.Diff.b_sr,
            beatmap.Diff.b_sr - beatmap.Diff.a_sr,
            beatmap.Diff.a_sr == 0 ? 1.0f : (beatmap.Diff.b_sr / beatmap.Diff.a_sr - 1)
        };
    }

    private static Sheet createSheet(string name) => new Sheet
    {
        Properties = new SheetProperties
        {
            Title = name,
            GridProperties = new GridProperties { FrozenRowCount = 1 }
        }
    };

    private static async Task<Google.Apis.Drive.v3.Data.File?> getSpreadSheetFile(DriveService service, string name)
    {
        var listRequest = service.Files.List();
        listRequest.Q = $"name = '{name}' and mimeType = '{sheet_mime}'";
        return (await listRequest.ExecuteAsync()).Files.FirstOrDefault();
    }

    private static string getModString(Mod[] mods) => mods.Any() ? string.Join(", ", mods.Select(m => m.Acronym.ToUpper())) : "NM";
    private static string getModString(APIMod[] mods) => mods.Any() ? string.Join(", ", mods.Select(m => m.Acronym.ToUpper())) : "NM";

    private static GoogleCredential getCredentials()
    {
        using (var stream = new FileStream(credentials_file, FileMode.Open, FileAccess.Read))
            return GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets, DriveService.Scope.Drive);
    }

    private static DriveService getDriveService(IConfigurableHttpClientInitializer credential)
    {
        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = new LongTimeoutInitializer(credential),
        });
    }

    private static SheetsService getSheetService(IConfigurableHttpClientInitializer credential)
    {
        return new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = new LongTimeoutInitializer(credential)
        });
    }
}
