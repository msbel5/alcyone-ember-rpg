namespace EmberCrpg.Domain.World
{
    /// <summary>P2 (DFU LegalRep-lite): a settlement's crime pressure. Reports and maulings
    /// raise it, days grind it down, and past the threshold the WHOLE watch sweeps.</summary>
    public sealed class SiteUnrestRecord
    {
        public EmberCrpg.Domain.Core.SiteId SiteId;
        public int Unrest;
        public long LastDecayDay;
    }
}
