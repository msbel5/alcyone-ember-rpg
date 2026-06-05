using System.Collections.Generic;

namespace EmberCrpg.Presentation.Ember.UI
{
    public sealed class CombatSpellActionRow
    {
        public CombatSpellActionRow(string actionId, string name, string school, int manaCost, bool enabled)
        {
            ActionId = actionId ?? string.Empty;
            Name = name ?? string.Empty;
            School = school ?? string.Empty;
            ManaCost = manaCost;
            Enabled = enabled;
        }

        public string ActionId { get; }
        public string Name { get; }
        public string School { get; }
        public int ManaCost { get; }
        public bool Enabled { get; }
    }

    public sealed class CombatScreenState
    {
        public CombatScreenState(
            bool hasEncounter,
            string playerName,
            int playerHealth,
            int playerHealthMax,
            int playerFatigue,
            int playerFatigueMax,
            int playerMana,
            int playerManaMax,
            string enemyName,
            int enemyHealth,
            int enemyHealthMax,
            string lastEventLine,
            IReadOnlyList<CombatSpellActionRow> spells)
        {
            HasEncounter = hasEncounter;
            PlayerName = playerName ?? string.Empty;
            PlayerHealth = playerHealth;
            PlayerHealthMax = playerHealthMax;
            PlayerFatigue = playerFatigue;
            PlayerFatigueMax = playerFatigueMax;
            PlayerMana = playerMana;
            PlayerManaMax = playerManaMax;
            EnemyName = enemyName ?? string.Empty;
            EnemyHealth = enemyHealth;
            EnemyHealthMax = enemyHealthMax;
            LastEventLine = lastEventLine ?? string.Empty;
            Spells = spells ?? System.Array.Empty<CombatSpellActionRow>();
        }

        public bool HasEncounter { get; }
        public string PlayerName { get; }
        public int PlayerHealth { get; }
        public int PlayerHealthMax { get; }
        public int PlayerFatigue { get; }
        public int PlayerFatigueMax { get; }
        public int PlayerMana { get; }
        public int PlayerManaMax { get; }
        public string EnemyName { get; }
        public int EnemyHealth { get; }
        public int EnemyHealthMax { get; }
        public string LastEventLine { get; }
        public IReadOnlyList<CombatSpellActionRow> Spells { get; }
    }

    public interface ICombatScreenSource
    {
        CombatScreenState ReadCombatScreenState();
    }
}
