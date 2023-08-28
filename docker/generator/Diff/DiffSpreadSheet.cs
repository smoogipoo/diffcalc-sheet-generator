// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Generator.Generators;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Generator.Diff;

public class DiffSpreadSheet
{
    private const string credentials_file = "/credentials.json";
    private const string sheet_mime = "application/vnd.google-apps.spreadsheet";

    public readonly Spreadsheet SpreadSheet;
    private readonly SheetsService service;

    private DiffSpreadSheet(Spreadsheet spreadSheet, SheetsService service)
    {
        SpreadSheet = spreadSheet;
        this.service = service;
    }

    public async Task AddSheet(IGenerator generator, object[][] rows)
    {
        Console.WriteLine($"Creating sheet '{generator.Name}'...");

        // Create sheet
        BatchUpdateSpreadsheetResponse response = await service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
        {
            Requests = new List<Request>
            {
                new Request
                {
                    AddSheet = new AddSheetRequest
                    {
                        Properties = new SheetProperties
                        {
                            Title = generator.Name,
                            GridProperties = new GridProperties { FrozenRowCount = 1 }
                        }
                    }
                }
            }
        }, SpreadSheet.SpreadsheetId).ExecuteAsync();

        SheetProperties properties = response.Replies[0].AddSheet.Properties;

        // Embolden first row
        List<Request> updateRequests = new List<Request>
        {
            new Request
            {
                RepeatCell = new RepeatCellRequest
                {
                    Range = new GridRange
                    {
                        SheetId = properties.SheetId,
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
            }
        };

        // Update columns
        for (int i = 0; i < generator.Columns.Length; i++)
        {
            if (generator.Columns[i].Width != null)
            {
                updateRequests.Add(new Request
                {
                    UpdateDimensionProperties = new UpdateDimensionPropertiesRequest
                    {
                        Range = new DimensionRange
                        {
                            SheetId = properties.SheetId,
                            Dimension = "COLUMNS",
                            StartIndex = i,
                            EndIndex = i + 1,
                        },
                        Properties = new DimensionProperties
                        {
                            PixelSize = generator.Columns[i].Width
                        },
                        Fields = "pixelSize"
                    }
                });
            }

            if (generator.Columns[i].Type == ColumnType.Percentage)
            {
                updateRequests.Add(new Request
                {
                    RepeatCell = new RepeatCellRequest
                    {
                        Range = new GridRange
                        {
                            SheetId = properties.SheetId,
                            StartRowIndex = 0,
                            StartColumnIndex = i,
                            EndColumnIndex = i + 1
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
                });
            }
        }

        await service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest
        {
            Requests = updateRequests
        }, SpreadSheet.SpreadsheetId).ExecuteAsync();

        var request = service.Spreadsheets.Values.Update(new ValueRange
        {
            Values = rows
                     // Add column headers
                     .Prepend(generator.Columns.Select(c => c.Title).Cast<object>())
                     .Select(r => (IList<object>)r.ToList())
                     .ToList()
        }, SpreadSheet.SpreadsheetId, properties.Title);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        await request.ExecuteAsync();

        Console.WriteLine($"'{properties.Title}' finished!");
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
            Properties = new SpreadsheetProperties { Title = spreadSheetName }
        }).ExecuteAsync();

        // Share file publicly.
        existingFile = (await getSpreadSheetFile(driveService, spreadSheetName))!;
        await driveService.Permissions.Create(new Permission
        {
            Type = "anyone",
            Role = "reader"
        }, existingFile.Id).ExecuteAsync();

        return new DiffSpreadSheet(spreadSheet, sheetService);
    }

    private static async Task<Google.Apis.Drive.v3.Data.File?> getSpreadSheetFile(DriveService service, string name)
    {
        var listRequest = service.Files.List();
        listRequest.Q = $"name = '{name}' and mimeType = '{sheet_mime}'";
        return (await listRequest.ExecuteAsync()).Files.FirstOrDefault();
    }

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
