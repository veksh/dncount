// dependency
// - vscode: "solution explorer", right-click, "add project reference"
// - cli: `dotnet add stageweb reference stagestats`
// run: `dotnet run --project stageweb`

using Microsoft.AspNetCore.Http.HttpResults;
using RaceData;
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
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
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
    // conf: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/
    // var message = app.Configuration["MyKey"] ?? "undef";
    // var title = Configuration["Position:Title"];
    // var number = Configuration.GetValue<int>("NumberKey", 99)
    // env: builder.Configuration.AddEnvironmentVariables(prefix: "MYAPPENV_");
}
// app.UseHttpsRedirection();
app.UseCors();

// see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-8.0
app.Logger.LogInformation("The app started");

app.MapGet("/", (HttpResponse response) => {
    app.Logger.LogInformation("root page requested");
    string fpath = "data/index.html";
    try {
        var fStream = File.OpenRead(fpath);
        return Results.Stream(fStream, "text/html");
    } catch (IOException e) {
        app.Logger.LogError(e, "could not open index page");
        return Results.NotFound();
    }
});

HttpClient client = new();
// optional: string? gender, ... gender ?? "yes"
// curl http://localhost:5432/chartwebfly
app.MapGet("/chart", (int course, string filter="all:all") => {
    app.Logger.LogInformation($"started, course {course}");

    if (filter != "all:all" && filter.Split(":").Length != 2) {
        app.Logger.LogError("filter {filter} is malformed", filter);
        return Results.BadRequest($"filter {filter} is malformed");
    }

    // var baseUrl = "https://mockraceapi.fly.dev/middleware";
    var baseUrl = app.Configuration["DataAPI:BaseUrl"];

    string splitsUrl = string.Format("{0}/info/json?course={1}&setting=splits",
        baseUrl,
        course);
    string[] splitNames;
    string   splitNumbers;
    try {
        var splitsData = RaceData.UrlDataGetter.AllSplits(splitsUrl).
            Where(s => Convert.ToUInt32(s.Splitnr) > 100 && Convert.ToUInt32(s.Splitnr) < 1000);
        app.Logger.LogInformation(
            "parsed splits at {url}, got {count} records",
            splitsUrl, splitsData.ToArray().Length);
        splitNames = splitsData.Select(s => s.Splitname).ToArray();
        splitNumbers = string.Join(",", splitsData.Select(s => s.Splitnr));
    } catch (Exception e) {
        app.Logger.LogError(e, "could not fetch or parse splits at {url}", splitsUrl);
        return Results.BadRequest($"failed to parse splits: {e.Message}");
    }
    app.Logger.LogInformation($"will use split numbers {splitNumbers}");

    var detailNames = "start,gender,status";
    string dataUrl = string.Format("{0}/result/json?course={1}&splitnr={2}&detail={3}",
        baseUrl,
        course,
        splitNumbers,
        detailNames);

    List<Dictionary<string, string>> pDicts;
    try {
        pDicts = RaceData.UrlDataGetter.DictFromUrl(dataUrl);
        app.Logger.LogInformation(
            "parsed data at {url}, got {count} records",
            dataUrl, pDicts.Count);
    } catch (Exception e) {
        app.Logger.LogError(e, "could not fetch or parse data at {url}", dataUrl);
        return Results.BadRequest($"failed to parse course data: {e.Message}");
    }

    // filter for good status (if present)
    // for (int i = 0; i < pDicts.Count; i++) {pDicts[i] = ...}
    pDicts = pDicts.
        Where(p => p.GetValueOrDefault("status", "-") == "-").
        ToList();
    // now trim _Time suffix from keys if present
    pDicts = pDicts.
        Select(pd => pd.ToDictionary(
            pair => pair.Key.EndsWith("_Time")
                ? pair.Key.Substring(0, pair.Key.Length - 5)
                : pair.Key,
            pair => pair.Value)).
        ToList();
    // and finally apply field-level filter
    if (filter != "all:all") {
        var f = filter.Split(":");
        pDicts = pDicts.
            Where(p => p.GetValueOrDefault(f[0], "!" + f[1]) == f[1]).
            ToList();
        app.Logger.LogInformation(
            "filtered data with {filter}, {count} records matched",
            filter, pDicts.Count);
    }

    var sCounter = new StageCounter(splitNames);
    foreach (var p in pDicts) {
        sCounter.AddParticipant(p);
    }

    var splits = splitNames.
        Select(splitName =>
            new RaceData.RaceStage(splitName, sCounter.GetCount(splitName))).
        ToList();
    var states = Enum.
        GetValues(typeof(ParticipantStatus)).Cast<ParticipantStatus>().
        Select(pStatus =>
            new RaceData.RaceStatus(pStatus.ToString(), sCounter.GetStatusCount(pStatus))
        ).
        ToList();
    var res = new RaceData.RaceInfo(splits, states);

    app.Logger.LogInformation("done");
    return Results.Ok(res);
})
.WithName("chart")
.WithOpenApi();

app.MapGet("/courses", () => {
    app.Logger.LogInformation($"started");

    var baseUrl = app.Configuration["DataAPI:BaseUrl"];

    string coursesUrl = string.Format("{0}/info/json?setting=courses",
        baseUrl);
    var allCoursesInfo = new List<CourseInfo>{};
    try {
        var coursesData = RaceData.UrlDataGetter.AllCourses(coursesUrl);
        app.Logger.LogInformation(
            "parsed courses at {url}, got {count} records",
            coursesUrl, coursesData.ToArray().Length);
        allCoursesInfo = coursesData.
            Select(cd => new CourseInfo(cd.Coursename, Convert.ToUInt32(cd.Coursenr))).
            ToList();
    } catch (HttpRequestException e) {
        app.Logger.LogError(e, "could not fetch {url}", coursesUrl);
        return Results.NotFound($"failed to fetch courses: {e.Message}");
    } catch (Exception e) {
        app.Logger.LogError(e, "could not parse splits at {url}", coursesUrl);
        return Results.BadRequest($"failed to parse courses: {e.Message}");
    }

    app.Logger.LogInformation("done");
    return Results.Ok(allCoursesInfo);
})
.WithName("courses")
.WithOpenApi();

app.MapGet("/splits", (int course) => {
    app.Logger.LogInformation($"started, course {course}");

    var baseUrl = app.Configuration["DataAPI:BaseUrl"];

    string splitsUrl = string.Format("{0}/info/json?course={1}&setting=splits",
        baseUrl,
        course);
    var allSplitsInfo = new List<SplitInfo>{};
    try {
        var splitsData = RaceData.UrlDataGetter.AllSplits(splitsUrl).
            Where(s => Convert.ToUInt32(s.Splitnr) > 100
                    && Convert.ToUInt32(s.Splitnr) < 1000);
        app.Logger.LogInformation(
            "parsed splits at {url}, got {count} records",
            splitsUrl, splitsData.ToArray().Length);
        allSplitsInfo = splitsData.
            Select(sd => new SplitInfo(sd.Splitname, Convert.ToUInt32(sd.Splitnr))).
            ToList();
    } catch (HttpRequestException e) {
        app.Logger.LogError(e, "could not fetch {url}", splitsUrl);
        return Results.NotFound($"failed to fetch splits: {e.Message}");
    } catch (Exception e) {
        app.Logger.LogError(e, "could not parse splits at {url}", splitsUrl);
        return Results.BadRequest($"failed to parse splits: {e.Message}");
    }

    app.Logger.LogInformation("done");
    return Results.Ok(allSplitsInfo);
})
.WithName("splits")
.WithOpenApi();

// var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
// app.Run($"http://localhost:{port}");
app.Run();

// fine-tune appearance: [JsonPropertyName("text")] [JsonInclude]
namespace RaceData {

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

        // make it generic already?
        public static List<CourseData> AllCourses(string url) {
            return client.
                GetFromJsonAsync<Dictionary<string, List<CourseData>>>(url).
                GetAwaiter().
                GetResult()!.
                First().
                Value;
        }

        public static List<SplitData> AllSplits(string url) {
            return client.
                GetFromJsonAsync<Dictionary<string, List<SplitData>>>(url).
                GetAwaiter().
                GetResult()!.
                First().
                Value;
        }
    }

    // src
    record SplitData (
        string Splitnr,
        string Splitname,
        string ID,
        string State,
        string ToD) {}

    record CourseData (
        string Coursenr,
        string Coursename,
        string Event,
        string Eventname,
        string Status,
        string Timeoffset,
        string Ordering,
        string Remark) {}

    // dst
    record RaceStage(string StageName, int NumRunners) {}
    record RaceStatus(string StatusName, int NumRunners) {}
    record RaceInfo(List<RaceStage> Stages, List<RaceStatus> States) {}
    record CourseInfo(string CourseName, uint CourseID) {}
    record SplitInfo(string SplitName, uint SplitID) {}
}

/* attic

app.MapGet("/chartfake", (int courseNo = 100) =>
{
    var stageNames = new string[]{"start", "middle", "finish"};
    var stages = stageNames.Select(stageName =>
        new RaceData.RaceStage(stageName, Random.Shared.Next(0, courseNo))
    ).ToList();
    var stateNames = new string[]{"not_started", "running", "done"};
    var states = stateNames.Select(stateName =>
        new RaceData.RaceStatus(stateName, Random.Shared.Next(0, courseNo))
    ).ToList();
    var res = new RaceData.RaceInfo(stages, states);
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
        raceData = new StreamReader($"data/{fileName}.json").ReadToEnd();
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
            new RaceData.RaceStage(stageName, sCounter.GetCount(stageName))).
        ToList();

    var states = Enum.
        GetValues(typeof(ParticipantStatus)).Cast<ParticipantStatus>().
        Select(pStatus =>
            new RaceData.RaceStatus(pStatus.ToString(), sCounter.GetStatusCount(pStatus))
        ).
        ToList();
    var res = new RaceData.RaceInfo(stages, states);
    app.Logger.LogInformation("processed file {fileName}", fileName);
    return Results.Ok(res);
})
.WithName("chartfile")
.WithOpenApi();

// optional: string? gender, ... gender ?? "yes"
app.MapGet("/chartwebstatic", (int courseNo = 101) =>
{
    app.Logger.LogInformation("started");

    string dataUrl = "https://xuhapage.s3.eu-west-2.amazonaws.com/participants.json";
    List<Dictionary<string, string>> pDicts;
    try {
        pDicts = RaceData.UrlDataGetter.DictFromUrl(dataUrl);
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
        var sDict = RaceData.UrlDataGetter.DictFromUrl(stagesUrl);
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
            new RaceData.RaceStage(stageName, sCounter.GetCount(stageName))).
        ToList();
    var states = Enum.
        GetValues(typeof(ParticipantStatus)).Cast<ParticipantStatus>().
        Select(pStatus =>
            new RaceData.RaceStatus(pStatus.ToString(), sCounter.GetStatusCount(pStatus))
        ).
        ToList();
    var res = new RaceData.RaceInfo(stages, states);

    app.Logger.LogInformation("done");
    return Results.Ok(res);
})
.WithName("chartwebstatic")
.WithOpenApi();

*/
