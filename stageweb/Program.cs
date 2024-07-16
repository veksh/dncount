// dependency
// - vscode: "solution explorer", right-click, "add project reference"
// - cli: `dotnet add stageweb reference stagestats`
// run: `dotnet run --project stageweb`

using Stage;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/chartdata", (int courseNo = 100) =>
{
    var stageNames = new string[]{"start", "middle", "finish"};
    var stages = stageNames.Select(stageName =>
        new Stageweb.RaceStage(stageName, Random.Shared.Next(0, 100))
    ).ToList();
    var stateNames = new string[]{"not_started", "running", "done"};
    var states = stateNames.Select(stateName =>
        new Stageweb.StatusInfo(stateName, Random.Shared.Next(0, 100))
    ).ToList();
    var res = new Stageweb.RaceInfo(stages, states);
    return res;
})
.WithName("chartdata")
.WithOpenApi();

app.Run();

// fine-tune appearance: [JsonPropertyName("text")] [JsonInclude]
namespace Stageweb {
    record RaceStage(string StageName, int NumRunners) {}
    record StatusInfo(string StatusName, int NumRunners) {}
    record RaceInfo(List<RaceStage> Stages, List<StatusInfo> States) {}
}
