using System.Collections.Immutable;
using ParticipantRecord = System.Collections.Generic.IDictionary<string, string>;

namespace Stage {

    // status of the runner
    public enum RecordStatus {
        WAITING,   // not started yet == zero time marks
        RUNNING,   // started but not finished == some time marks
        FINISHED,  // all stages passed == all time marks
        INVALID    // wrong order, not included in previous
    }

    public class StageCounter
    {
        // name of valid stages in right order
        public string[] StageNames { get; init; }
        // count by status
        private readonly Dictionary<RecordStatus, int> statusCount;
        // count by stage
        private readonly Dictionary<string, int> stageCount;

        public StageCounter(string[] StageNames) {
            this.StageNames = StageNames;
            stageCount = StageNames.ToDictionary(k => k, k => 0);
            // https://stackoverflow.com/questions/5583717/enum-to-dictionaryint-string-in-c-sharp
            statusCount = Enum.GetValues(typeof(RecordStatus))
                .Cast<RecordStatus>()
                .ToDictionary(t => t, t => 0);
        }

        public (RecordStatus, string) AddRecord(ParticipantRecord stageTimes) {
            return (RecordStatus.WAITING, "");
        }

        // meaning "passed stageName" i.e. "between stageName and next"
        // does not error on wrong stage name, just returns 0
        public int GetCount(string stageName) {
            return stageCount.GetValueOrDefault(stageName, 0);
        }

        public int GetStatusCount(RecordStatus status) {
            return statusCount[status];
        }

    }

}
