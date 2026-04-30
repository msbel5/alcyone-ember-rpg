using System.Collections.Generic;
using EmberCrpg.Domain.Core;

// Design note:
// EncounterState tracks Sprint 1's temporary bounded turn loop used only for the vertical slice.
// Inputs: player/enemy actor ids and resolved combat turn summaries.
// Outputs: deterministic turn ownership, finish state, and accumulated log lines.
// Bible reference: PRD temporary sprint deviation for FR-02.
namespace EmberCrpg.Domain.Combat
{
    /// <summary>Bounded one-vs-one encounter state for the slice-only turn compromise.</summary>
    public sealed class EncounterState
    {
        private readonly List<string> _logLines = new List<string>();

        public EncounterState(ActorId playerId, ActorId enemyId)
        {
            PlayerId = playerId;
            EnemyId = enemyId;
            PlayerActsNext = true;
        }

        public ActorId PlayerId { get; }
        public ActorId EnemyId { get; }
        public bool PlayerActsNext { get; set; }
        public bool IsFinished { get; private set; }
        public string WinnerName { get; private set; }
        public IReadOnlyList<string> LogLines => _logLines;

        public void AddLog(string line)
        {
            _logLines.Add(line);
        }

        public void Finish(string winnerName)
        {
            WinnerName = winnerName;
            IsFinished = true;
        }
    }
}
