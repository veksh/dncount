// dependency
// - vscode: "solution explorer", right-click, "add project reference"
// - cli: `dotnet add stageweb reference stagestats`
// run: `dotnet run --project stageweb`

using Stage;
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
    app.Logger.LogInformation("processeed");
    // or just `return res`;
    // alt: Results.NotFound()|UnprocessableEntity()|Content(data, "application/json");
    return Results.Ok(res);
})
.WithName("chartfake")
.WithOpenApi();

// optional: string? gender, ... gender ?? "yes"
app.MapGet("/chartfile", (string fileName = "run1") =>
{
    var raceData = new StreamReader($"{fileName}.json").ReadToEnd();
    var pDicts = JsonSerializer.Deserialize<Dictionary<string,List<Dictionary<string, string>>>>(raceData)!.First().Value;
    var stageNames = pDicts.First<Dictionary<string, string>>().Keys.ToArray();
    var sCounter = new StageCounter(stageNames);
    foreach (var p in pDicts) {
        sCounter.AddParticipant(p);
    }
    var stages = stageNames.Select(stageName =>
        new Stageweb.RaceStage(stageName, sCounter.GetCount(stageName))
    ).ToList();

    var states = Enum.GetValues(typeof(ParticipantStatus)).Cast<ParticipantStatus>().Select(pStatus =>
        new Stageweb.StatusInfo(pStatus.ToString(), sCounter.GetStatusCount(pStatus))
    ).ToList();
    var res = new Stageweb.RaceInfo(stages, states);
    app.Logger.LogInformation("processeed");
    // or just `return res`;
    // alt: Results.NotFound()|UnprocessableEntity()|Content(data, "application/json");
    return Results.Ok(res);
})
.WithName("chartfile")
.WithOpenApi();

// var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
// app.Run($"http://localhost:{port}");
app.Run();

// fine-tune appearance: [JsonPropertyName("text")] [JsonInclude]
namespace Stageweb {
    record RaceStage(string StageName, int NumRunners) {}
    record StatusInfo(string StatusName, int NumRunners) {}
    record RaceInfo(List<RaceStage> Stages, List<StatusInfo> States) {}
}
