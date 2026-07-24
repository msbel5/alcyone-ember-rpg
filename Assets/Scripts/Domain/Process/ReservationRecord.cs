// Design note:
// W32 EAT slice (docs/ruh/w32/02-decision-reservation.md §4.1): one active claim of
// "1 unit of this tag at this site, for this actor, until this minute". Stock is
// count-based, so the claim is count-based too — PursuitRecord-style public-field POCO.
namespace EmberCrpg.Domain.Process
{
    /// <summary>One active reservation-ledger row.</summary>
    public sealed class ReservationRecord
    {
        public ulong Id;            // from the ledger's PERSISTED counter; saved
        public ulong SiteId;        // StockpileComponent.SiteId
        public string ItemTag;      // "wheat" etc. (FoodTags universe)
        public ulong ActorId;
        public long UntilMinutes;   // GameTime.TotalMinutes; PursuitRecord.UntilMinutes precedent
    }
}
