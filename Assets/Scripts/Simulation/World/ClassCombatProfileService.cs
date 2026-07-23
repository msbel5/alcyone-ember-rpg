using EmberCrpg.Domain.Actors;

namespace EmberCrpg.Simulation.World
{
    /// <summary>
    /// PLAYTEST FIX ("siniflar anlamli degil"): realtime melee dice read ONLY the four combat
    /// numbers (accuracy/dodge/armor/baseDamage), and every class used to copy the same defaults,
    /// so a Warrior and a Scholar swung identically. Derive the four numbers from the class-shaped
    /// stat block (catalog percent scale, 25-75): might lands harder, agility lands truer.
    /// </summary>
    public static class ClassCombatProfileService
    {
        public static int DeriveAccuracy(EmberStatBlock s) => 4 + s.Agi / 5 + s.Ins / 10;
        public static int DeriveDodge(EmberStatBlock s) => 2 + s.Agi / 6 + s.Ins / 12;
        public static int DeriveArmor(EmberStatBlock s) => 1 + s.End / 30;
        public static int DeriveBaseDamage(EmberStatBlock s) => 2 + s.Mig / 12 + s.End / 40;
    }
}
