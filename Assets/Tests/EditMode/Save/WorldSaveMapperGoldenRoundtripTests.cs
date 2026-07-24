using System.Linq;
using System.Reflection;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>
    /// REFORM #3 (golden coverage for EVERY mapper field): a representative world goes
    /// ToData -> ToWorld -> ToData and the two DTOs must match FIELD BY FIELD via
    /// reflection. Any mapper that drops a field - the Home/DayAnchor class of bug -
    /// fails HERE, forever, without anyone remembering to write the specific test.
    /// </summary>
    public sealed class WorldSaveMapperGoldenRoundtripTests
    {
        [Test]
        public void RepresentativeWorld_DoubleRoundtrip_IsFieldIdentical()
        {
            var world = new WorldFactory().Create(roomSeed: 7);

            // Populate every NEWER collection so silence can't hide a dropped field.
            world.PlayerClassName = "Warrior";
            world.CompanionIds.Add(101UL);
            world.GuardPursuits.Add(new PursuitRecord { GuardId = 5, TargetId = 6, UntilMinutes = 999 });
            world.Critters.Add(new AmbientCritter
            { Id = 11, SiteId = new SiteId(1), Cell = new GridPosition(4, 5), Kind = "rat" });
            world.Rumors.Add(new RumorEntry
            { BornMinutes = 42, SiteId = new SiteId(1), Text = "golden tale" });
            world.RumorEventCursor = 3;
            world.SiteUnrest.Add(new SiteUnrestRecord
            { SiteId = new SiteId(1), Unrest = 4, LastDecayDay = 2 });

            var first = WorldSaveMapper.ToData(world);
            var back = WorldSaveMapper.ToWorld(first, new WorldFactory().Create(roomSeed: 7));
            var second = WorldSaveMapper.ToData(back);

            var diffs = new System.Collections.Generic.List<string>();
            foreach (var field in typeof(EmberCrpg.Data.Save.WorldSaveData)
                         .GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!ValuesEqual(field.GetValue(first), field.GetValue(second)))
                    diffs.Add(field.Name);
            }
            Assert.That(diffs, Is.Empty,
                "mapper drops or mutates these fields on roundtrip: " + string.Join(", ", diffs));
        }

        private static bool ValuesEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a is System.Collections.IEnumerable ea && !(a is string)
                && b is System.Collections.IEnumerable eb)
            {
                var la = ea.Cast<object>().ToList();
                var lb = eb.Cast<object>().ToList();
                if (la.Count != lb.Count) return false;
                for (int i = 0; i < la.Count; i++)
                    if (!ValuesEqual(la[i], lb[i])) return false;
                return true;
            }
            var type = a.GetType();
            if (type.IsPrimitive || a is string || type.IsEnum || a is decimal)
                return a.Equals(b);
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (!ValuesEqual(field.GetValue(a), field.GetValue(b))) return false;
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (prop.CanRead && prop.GetIndexParameters().Length == 0
                    && !ValuesEqual(prop.GetValue(a), prop.GetValue(b))) return false;
            return true;
        }
    }
}
