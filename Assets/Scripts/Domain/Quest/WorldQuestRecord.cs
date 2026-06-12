using EmberCrpg.Domain.Worldgen;

// Design note:
// WorldQuestRecord (F21) is the DATA of one generated world quest — template, giver, target,
// cargo, deadline, reward, lifecycle flags. Pure Domain: no Unity, no clocks, no I/O. The
// Simulation generator fills it deterministically; the presentation adapter holds the live
// list (F22 moves that list into WorldState + the save mapper).
namespace EmberCrpg.Domain.Quest
{
    /// <summary>The four DFU-style work templates every generated quest is built from.</summary>
    public enum WorldQuestTemplate
    {
        Fetch = 0,   // bring an item to the giver
        Kill = 1,    // fell a named outlaw
        Deliver = 2, // carry an item to another settlement
        Visit = 3,   // reach another settlement
    }

    /// <summary>One generated world quest — data only; behavior lives in the generator/adapter.</summary>
    public sealed class WorldQuestRecord
    {
        public QuestId Id;
        public WorldQuestTemplate Template;
        public NpcId GiverNpcId;
        public string GiverName = string.Empty;
        public SettlementId TargetSettlementId;
        public string TargetSettlementName = string.Empty;
        public NpcId TargetNpcId;
        public string TargetNpcName = string.Empty;
        public string ItemTemplateId = string.Empty;
        public int RewardGold;
        public int DeadlineDay; // absolute world day; past it the quest FAILS
        public bool Completed;
        public bool Failed;
        public string Title = string.Empty;
    }
}
