namespace EmberCrpg.Domain.World
{
    /// <summary>P1 RumorMill (DFU pattern): one line of town talk born from a REAL world event.</summary>
    public sealed class RumorEntry
    {
        public long BornMinutes;
        public EmberCrpg.Domain.Core.SiteId SiteId;
        public string Text;
    }
}
