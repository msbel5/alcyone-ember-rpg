namespace EmberCrpg.Domain.World
{
    /// <summary>
    /// P1 ambient life (the DF lesson: cheap agents, REAL consequences). A critter is not an
    /// ActorRecord - no needs, no memory, no schedule - just a kind, a cell and a site. Rats
    /// steal from the REAL stockpile (the shortage/price chain reacts by itself); cats hunt
    /// rats. Nothing here is choreography: every effect lands in shared world state.
    /// </summary>
    public sealed class AmbientCritter
    {
        public ulong Id;
        public EmberCrpg.Domain.Core.SiteId SiteId;
        public EmberCrpg.Domain.Actors.GridPosition Cell;
        /// <summary>"rat" | "cat"</summary>
        public string Kind;
    }
}
