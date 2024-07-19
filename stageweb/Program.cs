// dependency
// - vscode: "solution explorer", right-click, "add project reference"
// - cli: `dotnet add stageweb reference stagestats`
// run: `dotnet run --project stageweb`

using Stage;
using System.Runtime.CompilerServices;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => {
    options.AddDefaultPolicy (
        policy => {
    });
});
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.WriteIndented = true;
    // options.SerializerOptions.IncludeFields = true;
});

// cons log: https://learn.microsoft.com/en-us/dotnet/core/extensions/console-log-formatter
// builder.Logging.AddJsonConsole();
builder.Logging.AddSimpleConsole(options => {
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss "; // "yyyy-MM-dd HH:mm:ss zzz"
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
    // conf: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0
    // var message = app.Configuration["MyKey"] ?? "undef";
    // var title = Configuration["Position:Title"];
    // var number = Configuration.GetValue<int>("NumberKey", 99)
    // env: builder.Configuration.AddEnvironmentVariables(prefix: "MYAPPENV_");
}
// app.UseHttpsRedirection();
app.UseCors();

// see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-8.0
app.Logger.LogInformation("The app started");

app.MapGet("/chartfake", (int courseNo = 100) =>
{
    var stageNames = new string[]{"start", "middle", "finish"};
    var stages = stageNames.Select(stageName =>
        new Stageweb.RaceStage(stageName, Random.Shared.Next(0, courseNo))
    ).ToList();
    var stateNames = new string[]{"not_started", "running", "done"};
    var states = stateNames.Select(stateName =>
        new Stageweb.StatusInfo(stateName, Random.Shared.Next(0, courseNo))
    ).ToList();
    var res = new Stageweb.RaceInfo(stages, states);
    app.Logger.LogInformation("returned fake data");
    // or just `return res`;
    // alt: Results.NotFound()|UnprocessableEntity()|Content(data, "application/json");
    return Results.Ok(res);
})
.WithName("chartfake")
.WithOpenApi();

// optional: string? gender, ... gender ?? "yes"
app.MapGet("/chartfile", (string fileName = "run1") =>
{
    string raceData = "";
    try {
        raceData = new StreamReader($"{fileName}.json").ReadToEnd();
    } catch (IOException e) {
        app.Logger.LogError(e, "could not open file {fileName}", fileName);
        return Results.NotFound();
    }
    var pDicts = JsonSerializer.Deserialize<Dictionary<string,List<Dictionary<string, string>>>>(raceData)!.First().Value;
    var stageNames = pDicts.First().Keys.ToArray();
    var sCounter = new StageCounter(stageNames);
    foreach (var p in pDicts) {
        sCounter.AddParticipant(p);
    }
    var stages = stageNames.
        Select(stageName =>
            new Stageweb.RaceStage(stageName, sCounter.GetCount(stageName))).
        ToList();

    var states = Enum.
        GetValues(typeof(ParticipantStatus)).Cast<ParticipantStatus>().
        Select(pStatus =>
            new Stageweb.StatusInfo(pStatus.ToString(), sCounter.GetStatusCount(pStatus))
        ).
        ToList();
    var res = new Stageweb.RaceInfo(stages, states);
    app.Logger.LogInformation("processed file {fileName}", fileName);
    return Results.Ok(res);
})
.WithName("chartfile")
.WithOpenApi();

// optional: string? gender, ... gender ?? "yes"
app.MapGet("/chartweb", (int courseNo = 101) =>
{
    app.Logger.LogInformation("started");

    string dataUrl = "https://xuhapage.s3.eu-west-2.amazonaws.com/participants.json";
    List<Dictionary<string, string>> pDicts;
    try {
        pDicts = Stageweb.UrlDataGetter.DictFromUrl(dataUrl);
        app.Logger.LogInformation(
            "parsed data at {url}, got {count} records",
            dataUrl, pDicts.Count);
    } catch (Exception e) {
        app.Logger.LogError(e, "could not fetch or parse data at {url}", dataUrl);
        return Results.BadRequest(e);
    }

    string stagesUrl = "https://xuhapage.s3.eu-west-2.amazonaws.com/stages.json";
    string[] stageNames;
    try {
        var sDict = Stageweb.UrlDataGetter.DictFromUrl(stagesUrl);
        stageNames = sDict.Select(d => d["name"]).ToArray();
        app.Logger.LogInformation(
            "parsed stages at {url}, got {count} records",
            stagesUrl, stageNames.Length);
    } catch (Exception e) {
        app.Logger.LogError(e, "could not fetch or parse stages at {url}", stagesUrl);
        return Results.BadRequest(e);
    }

    var sCounter = new StageCounter(stageNames);
    foreach (var p in pDicts) {
        sCounter.AddParticipant(p);
    }

    var stages = stageNames.
        Select(stageName =>
            new Stageweb.RaceStage(stageName, sCounter.GetCount(stageName))).
        ToList();
    var states = Enum.
        GetValues(typeof(ParticipantStatus)).Cast<ParticipantStatus>().
        Select(pStatus =>
            new Stageweb.StatusInfo(pStatus.ToString(), sCounter.GetStatusCount(pStatus))
        ).
        ToList();
    var res = new Stageweb.RaceInfo(stages, states);

    app.Logger.LogInformation("done");
    return Results.Ok(res);
})
.WithName("chartweb")
.WithOpenApi();

// var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
// app.Run($"http://localhost:{port}");
app.Run();

// fine-tune appearance: [JsonPropertyName("text")] [JsonInclude]
namespace Stageweb {

    internal class UrlDataGetter {
        static readonly HttpClient client = new();
        // throws an exception if not found, unparseable, empty, wrong structure etc
        public static List<Dictionary<string, string>> DictFromUrl(string url) {
            return client.
                GetFromJsonAsync<Dictionary<string,List<Dictionary<string, string>>>>(url).
                GetAwaiter().
                GetResult()!.
                First().
                Value;
        }
    }

    record RaceStage(string StageName, int NumRunners) {}
    record StatusInfo(string StatusName, int NumRunners) {}
    record RaceInfo(List<RaceStage> Stages, List<StatusInfo> States) {}
}
