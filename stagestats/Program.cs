// See https://aka.ms/new-console-template for more information

using Stage;

var scount = new StageCounter(["raz", "dva"]);
var res = scount.AddParticipant(new Dictionary<string, string>{["raz"] = "17:15", ["dva"] = "-"});
Console.WriteLine($"all ok: {res}");
