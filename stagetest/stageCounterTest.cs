namespace StageTest;

[TestClass]
public class StageCounterTest {

    [TestMethod]
    public void TestGetStatusSimple(){
        var stageNames = new string[]{"raz", "dva"};
        var scount = new Stage.StageCounter(stageNames);

        var (res, msg) = scount.GetStatus(new Dictionary<string, string>{
            ["raz"] = "10:00",
            ["dva"] = "-",
            ["tri"] = "extra"});
        Assert.AreEqual<string>(res.StageName, "raz");
        Assert.AreEqual<Stage.StatusCheckResult>(
            res,
            new Stage.StatusCheckResult(
                Stage.ParticipantStatus.RUNNING,
                "raz",
                "10:00"
            ));
    }
}
