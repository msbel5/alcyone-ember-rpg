using System;

namespace EmberCrpg.Simulation.AiDm
{
    /// <summary>
    /// Per-tick budget counter for LLM flavour calls. Rejects calls past the
    /// cap; callers fall back to deterministic template text. Phase 12 Atom 7.
    /// </summary>
    public sealed class FlavourBudget
    {
        private int _spent;
        private int _capPerTick;

        public FlavourBudget(int capPerTick)
        {
            if (capPerTick < 0)
                throw new ArgumentOutOfRangeException(nameof(capPerTick), "Cap must be non-negative.");
            _capPerTick = capPerTick;
        }

        public int CapPerTick => _capPerTick;
        public int Spent => _spent;
        public int Remaining => Math.Max(0, _capPerTick - _spent);

        /// <summary>Tries to reserve one flavour call. Returns true on success.</summary>
        public bool TryReserve()
        {
            if (_spent >= _capPerTick) return false;
            _spent++;
            return true;
        }

        /// <summary>Resets spent counter at the start of a new tick.</summary>
        public void ResetForTick()
        {
            _spent = 0;
        }

        /// <summary>Updates the per-tick cap (e.g. via settings change).</summary>
        public void UpdateCap(int newCap)
        {
            if (newCap < 0)
                throw new ArgumentOutOfRangeException(nameof(newCap));
            _capPerTick = newCap;
        }
    }
}
