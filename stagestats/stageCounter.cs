
using ParticipantRecord = System.Collections.Generic.IDictionary<string, string>;

namespace Stage {

    public enum RecordStatus {
        NOT_STARTED,
        RUNNING,
        FINISHED,
        INVALID
    }

    public class StageCounter(string[] StageNames)
    {

        public string[] StageNames { get; init; } = StageNames;

        private Dictionary<string, int> counts = new Dictionary<string, int>{
            ["_total"]   = 0,
            ["_invalid"] = 0,
        };

        public (RecordStatus, string) AddRecord(ParticipantRecord stageTimes) {
            return (RecordStatus.NOT_STARTED, "");
        }

        public int GetStageCount(string stageName) {
            return counts.GetValueOrDefault(stageName, 0);
        }

    }

}
