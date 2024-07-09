// See https://aka.ms/new-console-template for more information
using System.Text.Json;
using System.Text.Json.Serialization;

using Stage;

var stageNames = new string[]{"raz", "dva"};
var scount = new StageCounter(stageNames);

var res = scount.GetStatus(new Dictionary<string, string>{["raz"] = "10:00", ["dva"] = "-", ["tri"] = "-"});
Console.WriteLine($"should be raz: {res}");

var res1 = scount.AddParticipant(new Dictionary<string, string>{["raz"] = "-", ["dva"] = "-"});
Console.WriteLine($"all ok for first: {res1}");
var res2 = scount.AddParticipant(new Dictionary<string, string>{["raz"] = "17:15", ["dva"] = "-"});
Console.WriteLine($"all ok for second: {res2}");
var res3 = scount.AddParticipant(new Dictionary<string, string>{["raz"] = "07:15", ["dva"] = "18:00"});
Console.WriteLine($"all ok for third: {res3}");
var res4 = scount.AddParticipant(new Dictionary<string, string>{["raz"] = "-", ["dva"] = "18:00"});
Console.WriteLine($"here comes the invalid: {res4}");

Console.WriteLine(string.Join(", ", stageNames.Select(sn => $"{sn}: {scount.GetCount(sn)}")));
Console.WriteLine(string.Join(", ", Enum.GetValues(typeof(ParticipantStatus))
  .Cast<ParticipantStatus>()
  .Select(sn => $"{sn}: {scount.GetStatusCount(sn)}")));

// or cmdline arg, or env var
// or var httpClient = new HttpClient(); var guys = await httpClient.GetFromJsonAsync<dt>("https://somewhere");
// or <string,object> and o.ToString()/Int32.TryParse(o.ToString())
var participants = new StreamReader("participants.json").ReadToEnd();
var pDicts = JsonSerializer.Deserialize<Dictionary<string,List<Dictionary<string, string>>>>(participants)!.First().Value;

// Console.WriteLine($"it is: {JsonSerializer.Serialize(pDict)}");
var stageNames2 = new string[]{"start", "stage1", "finish"};
var scount2 = new StageCounter(stageNames2);
foreach (var p in pDicts) {
  scount2.AddParticipant(p);
}
Console.WriteLine(string.Join(", ", stageNames2.Select(sn => $"{sn}: {scount2.GetCount(sn)}")));
