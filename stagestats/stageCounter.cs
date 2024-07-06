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
    public record struct StatusCheckResult (
        ParticipantStatus Status,
        string? StageName = null,
        string? StageTime = null
    );

    public class StageCounter {
        // name of valid stages in right order
        public string[] StageNames { get; init; }
        // count by status
        private readonly Dictionary<ParticipantStatus, int> statusCount;
        // count by stage; could be not int but struct with "first time reached" etc
        private readonly Dictionary<string, int> stageCount;

        // private readonly StatusCheckResult INVALID_STATUS = new(ParticipantStatus.INVALID);

        public StageCounter(string[] StageNames) {
            this.StageNames = StageNames;
            stageCount = StageNames.ToDictionary(k => k, k => 0);
            // https://stackoverflow.com/questions/5583717/enum-to-dictionaryint-string-in-c-sharp
            statusCount = Enum.GetValues(typeof(ParticipantStatus))
                .Cast<ParticipantStatus>()
                .ToDictionary(t => t, t => 0);
        }

        // could be static with StageNames passed
        public (StatusCheckResult, string) GetStatus(ParticipantRecord newRecord) {
            var res = new StatusCheckResult(ParticipantStatus.WAITING);
            var unreachedStageSeen = false;
            foreach(string currentStageName in StageNames) {
                if (newRecord.TryGetValue(currentStageName, out string? currentStageTime)) {
                    if (currentStageTime != "-") {
                        if (unreachedStageSeen) {
                            return (new StatusCheckResult(ParticipantStatus.INVALID),
                                $"gap before {currentStageName}");
                        }
                        if (res.StageName != null && String.Compare(currentStageTime, res.StageTime) < 0) {
                            return (new StatusCheckResult(ParticipantStatus.INVALID),
                                $"{res.StageName} time > {currentStageName} time");
                        }
                        // could update in-place, but typically occurs only once
                        res = new StatusCheckResult(ParticipantStatus.RUNNING, currentStageName, currentStageTime);
                    } else {
                        unreachedStageSeen = true;
                    }
                } else {
                    return (new StatusCheckResult(ParticipantStatus.INVALID),
                        $"stage {currentStageName} is missed");
                }
            }

            if (res.Status == ParticipantStatus.RUNNING && !unreachedStageSeen) {
                res.Status = ParticipantStatus.FINISHED;
            }
            return (res, "all ok");
        }

        public StatusCheckResult AddParticipant(ParticipantRecord newRecord) {
            var (res, msg) = GetStatus(newRecord);
            statusCount[res.Status] += 1;
            if (res.Status == ParticipantStatus.RUNNING) {
                stageCount[res.StageName!] += 1;
            }
            if (res.Status == ParticipantStatus.INVALID) {
                var recFields = "\"" + string.Join(",", newRecord.Select(kvp => $"{kvp.Key}:{kvp.Value}")) + "\"";
                Console.WriteLine($"WARN: invalid participant {recFields}: {msg}");
            }
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
