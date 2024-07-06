// See https://aka.ms/new-console-template for more information

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
