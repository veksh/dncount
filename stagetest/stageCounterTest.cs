namespace StageTest;

[TestClass]
public class StageCounterTest {

    [TestMethod]
    public void GetStatusSimple(){
        var stageNames = new string[]{"raz", "dva"};
        var scount = new Stage.StageCounter(stageNames);

        var (res, _) = scount.GetStatus(new Dictionary<string, string>{
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

    [TestMethod]
    public void GetStatusInvalidOrder(){
        var stageNames = new string[]{"raz", "dva", "tri"};
        var scount = new Stage.StageCounter(stageNames);

        var (res, msg) = scount.GetStatus(new Dictionary<string, string>{
            ["raz"] = "10:00",
            ["dva"] = "09:00",
            ["tri"] = "-"});
        Assert.AreEqual<Stage.ParticipantStatus>(res.Status, Stage.ParticipantStatus.INVALID);
        // just for example -- should not really check the message here
        StringAssert.Contains(msg, "time");
    }

    [TestMethod]
    public void GetStatusCounts(){
        // Arrange
        var stageNames = new string[]{"raz", "dva"};
        var scount = new Stage.StageCounter(stageNames);
        // good options for invalids: null, String.Empty, "utf стринг", new List<string>{"a", b"}
        List<Dictionary<string, string>> participants = [
            new() {
                {"raz", "-"},
                {"dva", "-"},
            },
            new() {
                {"raz", "10:00"},
                {"dva", "-"},
            },
            new() {
                {"raz", "10:00"},
                {"dva", "11:00"},
            },
            new() {
                {"raz", "-"},
                {"dva", "18:00"},
            },
        ];

        // Act
        foreach (var p in participants) {
            scount.AddParticipant(p);
        }

        // Assert
        foreach (Stage.ParticipantStatus ps in Enum.GetValues(typeof(Stage.ParticipantStatus))) {
            Assert.AreEqual(scount.GetStatusCount(ps), 1);
        }
    }
}
