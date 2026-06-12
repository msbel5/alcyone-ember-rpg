using System.Collections.Generic;

// Design note:
// MainQuestState is the F31 three-act spine: ACT 1 — gather the ancient inscription pieces from
// the delves (one per delve, the requirement adapts to worlds with fewer delves); ACT 2 — bring
// them to the capital's sage; ACT 3 — descend the FINAL delve and fell its Warden. Pure Domain
// state machine: deterministic, no Unity dependency, save-mapped like every other world store.
// Out-of-order calls REFUSE instead of silently advancing — the acts are a spine, not a checklist.
namespace EmberCrpg.Domain.Quest
{
    /// <summary>The three-act main quest spine. Acts: 1 = inscriptions, 2 = the sage,
    /// 3 = the final Warden, 4 = complete.</summary>
    public sealed class MainQuestState
    {
        public int Act = 1;
        public int RequiredInscriptions = 3;
        public ulong FinalDelveId;
        /// <summary>Settlement ids of delves that already yielded their piece (one each).</summary>
        public List<ulong> ClaimedDelveIds = new List<ulong>();

        public int InscriptionsFound => ClaimedDelveIds?.Count ?? 0;
        public bool IsComplete => Act >= 4;

        public void EnsureInvariants()
        {
            ClaimedDelveIds ??= new List<ulong>();
            if (Act < 1) Act = 1;
            if (Act > 4) Act = 4;
            if (RequiredInscriptions < 1) RequiredInscriptions = 1;
        }

        /// <summary>Configure at world seed: the requirement adapts to small worlds (a one-delve
        /// world still has a complete spine) and the final delve is pinned deterministically.</summary>
        public void Configure(int delveCount, ulong finalDelveId)
        {
            RequiredInscriptions = delveCount < 3 ? (delveCount < 1 ? 1 : delveCount) : 3;
            FinalDelveId = finalDelveId;
        }

        /// <summary>ACT 1: a delve chest yields its inscription piece — once per delve.</summary>
        public bool TryFindInscription(ulong delveSettlementId, out string line)
        {
            EnsureInvariants();
            if (Act != 1)
            {
                line = null;
                return false;
            }
            if (ClaimedDelveIds.Contains(delveSettlementId))
            {
                line = null; // this delve already gave its piece
                return false;
            }
            ClaimedDelveIds.Add(delveSettlementId);
            if (InscriptionsFound >= RequiredInscriptions)
            {
                Act = 2;
                line = $"The inscription is whole ({InscriptionsFound}/{RequiredInscriptions}). " +
                       "The capital's sage must read it.";
            }
            else
            {
                line = $"An ancient inscription piece ({InscriptionsFound}/{RequiredInscriptions}).";
            }
            return true;
        }

        /// <summary>ACT 2: the sage reads the gathered inscription.</summary>
        public bool TryConsultSage(out string line)
        {
            EnsureInvariants();
            if (Act != 2)
            {
                line = null;
                return false;
            }
            Act = 3;
            line = "The sage traces the joined glyphs: the warden under the old stones keeps " +
                   "the ember's name. Fell it, and the road to the ember opens.";
            return true;
        }

        /// <summary>ACT 3: the FINAL delve's Warden falls.</summary>
        public bool TryFellFinalWarden(ulong delveSettlementId, out string line)
        {
            EnsureInvariants();
            if (Act != 3 || delveSettlementId != FinalDelveId)
            {
                line = null;
                return false;
            }
            Act = 4;
            line = "The Warden falls and the old stones go quiet. The ember's name is yours.";
            return true;
        }
    }
}
