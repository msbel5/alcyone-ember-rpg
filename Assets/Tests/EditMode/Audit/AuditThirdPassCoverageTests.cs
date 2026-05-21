using System;
using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Combat;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Audit
{
    /// <summary>
    /// Codex audit (third pass) coverage gaps. Pins new behavior added in
    /// this pass: SliceSaveMapper null guard, mutator lifecycles, query
    /// accessors flagged in dimension G.
    /// </summary>
    public sealed class AuditThirdPassCoverageTests
    {
        // ----- SliceSaveMapper.ToData null guard (A-P3) -----
        [Test]
        public void SliceSaveMapper_ToData_NullWorldThrows()
        {
            Assert.Throws<ArgumentNullException>(() => SliceSaveMapper.ToData(null));
        }

        // ----- RealtimeCombatState.TogglePaused (G-P2) -----
        [Test]
        public void RealtimeCombatState_TogglePaused_Flips()
        {
            var s = new EmberCrpg.Simulation.Combat.RealtimeCombatState();
            Assert.That(s.IsPaused, Is.False);
            s.TogglePaused();
            Assert.That(s.IsPaused, Is.True);
            s.TogglePaused();
            Assert.That(s.IsPaused, Is.False);
        }

        // ----- VitalStat.Refill (G-P3) -----
        [Test]
        public void VitalStat_Refill_SetsCurrentToMax()
        {
            var vital = new VitalStat(2, 10);
            var refilled = vital.Refill();
            Assert.That(refilled.Current, Is.EqualTo(10));
            Assert.That(refilled.Max, Is.EqualTo(10));
        }

        // ----- EquipmentState.IsEquipped (G-P3) -----
        [Test]
        public void EquipmentState_IsEquipped_TrueAfterEquip()
        {
            var eq = new EquipmentState();
            var itemId = new ItemId(42UL);
            Assert.That(eq.IsEquipped(itemId), Is.False);
            eq.Equip(EquipmentSlot.Weapon, itemId);
            Assert.That(eq.IsEquipped(itemId), Is.True);
            eq.Unequip(EquipmentSlot.Weapon);
            Assert.That(eq.IsEquipped(itemId), Is.False);
        }

        [Test]
        public void EquipmentState_IsEquipped_EmptyItemIdAlwaysFalse()
        {
            var eq = new EquipmentState();
            Assert.That(eq.IsEquipped(new ItemId(0UL)), Is.False);
        }

        // ----- EquipmentState.EnumerateEquipped (G-P3) -----
        [Test]
        public void EquipmentState_EnumerateEquipped_OnlyNonEmptyInStableOrder()
        {
            var eq = new EquipmentState();
            eq.Equip(EquipmentSlot.Weapon, new ItemId(7UL));
            var pairs = System.Linq.Enumerable.ToList(eq.EnumerateEquipped());
            Assert.That(pairs.Count, Is.EqualTo(1));
            Assert.That(pairs[0].Value.Value, Is.EqualTo(7UL));
        }

        // ----- SpellResolver wiring (D-P2) — pin existence + basic ok path -----
        [Test]
        public void SpellResolver_HandlerRegistry_HasHandlerLookup()
        {
            var handlers = new EmberCrpg.Simulation.Magic.EffectOperationHandlers();
            handlers.Register(EmberCrpg.Domain.Magic.EffectOperationKind.DirectDamage, _ => 1);
            Assert.That(handlers.HasHandler(EmberCrpg.Domain.Magic.EffectOperationKind.DirectDamage), Is.True);
            Assert.That(handlers.HasHandler(EmberCrpg.Domain.Magic.EffectOperationKind.DirectRestore), Is.False);
        }

        // ----- SliceWorldFactory smoke: produces a world with seed >= 0 -----
        [Test]
        public void SliceWorldFactory_Create_ProducesNonNullWorld()
        {
            var world = new SliceWorldFactory().Create(roomSeed: 1);
            Assert.That(world, Is.Not.Null);
            Assert.That(world.Actors, Is.Not.Null);
            Assert.That(world.Actors.Count, Is.GreaterThan(0));
        }
    }
}
