// See https://aka.ms/new-console-template for more information

using Stage;

var scount = new StageCounter(["raz", "dva", "tri"]);
scount.AddRecord(new Dictionary<string, string>{["raz"] = "17:15", ["dva"] = "-"});
Console.WriteLine($"all ok: {scount.GetCount("raz")}");
