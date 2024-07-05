using System.Collections.Immutable;
using ParticipantRecord = System.Collections.Generic.IDictionary<string, string>;

namespace Stage {

    // status of the runner
    public enum ParticipantStatus {
        WAITING,   // not started yet == zero time marks
        RUNNING,   // started but not finished == some time marks
        FINISHED,  // all stages passed == all time marks
        INVALID    // wrong order, not included in previous
    }

    // readonly?
    public record struct StatusCheckResult(ParticipantStatus Status, string? StageName = null, string? StageTime = null);

    public class StageCounter
    {
        // name of valid stages in right order
        public string[] StageNames { get; init; }
        // count by status
        private readonly Dictionary<ParticipantStatus, int> statusCount;
        // count by stage
        private readonly Dictionary<string, int> stageCount;

        private readonly StatusCheckResult INVALID_STATUS = new(ParticipantStatus.INVALID);

        public StageCounter(string[] StageNames) {
            this.StageNames = StageNames;
            stageCount = StageNames.ToDictionary(k => k, k => 0);
            // https://stackoverflow.com/questions/5583717/enum-to-dictionaryint-string-in-c-sharp
            statusCount = Enum.GetValues(typeof(ParticipantStatus))
                .Cast<ParticipantStatus>()
                .ToDictionary(t => t, t => 0);
        }

        public StatusCheckResult AddParticipant(ParticipantRecord newRecord) {

            var res = new StatusCheckResult(ParticipantStatus.WAITING);
            var unreachedStageSeen = false;
            foreach(string currentStageName in StageNames) {
                if (!newRecord.ContainsKey(currentStageName)) {
                    Console.WriteLine($"WARN: invalid participant {newRecord}: {currentStageName} is missed");
                    res = INVALID_STATUS;
                    break;
                }
                var currentStageTime = newRecord[currentStageName];
                if (currentStageTime != "-") {
                    if (unreachedStageSeen) {
                        Console.WriteLine($"WARN: invalid participant {newRecord}: gap between {res.StageName} and {currentStageName}");
                        res = INVALID_STATUS;
                        break;
                    }
                    if (res.StageName != null && String.Compare(currentStageTime, res.StageTime) < 0) {
                        Console.WriteLine($"WARN: invalid participant {newRecord}: {res.StageName} time > {currentStageName} time");
                        res = INVALID_STATUS;
                        break;
                    }
                    res = new StatusCheckResult(ParticipantStatus.RUNNING, currentStageName, currentStageTime);
                } else {
                    unreachedStageSeen = true;
                }
            }

            if (!unreachedStageSeen) {
                res.Status = ParticipantStatus.FINISHED;
            }

            statusCount[res.Status] += 1;
            return res;
        }

        // meaning "passed stageName" i.e. "between stageName and next"
        // does not error on wrong stage name, just returns 0
        public int GetCount(string stageName) {
            return stageCount.GetValueOrDefault(stageName, 0);
        }

        public int GetStatusCount(ParticipantStatus status) {
            return statusCount[status];
        }

    }

}
