using EmberCrpg.Data.Save;
using EmberCrpg.Domain.Actors;
using EmberCrpg.Domain.Core;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>
    /// PLAYTEST pin: the mapper used to DROP Home/DayAnchor (the ctor re-defaulted both to the
    /// saved position), so one night save collapsed every villager's Home onto the sleeping
    /// pile at the town centre — forever. The roundtrip must keep both anchors.
    /// </summary>
    public sealed class ActorSaveMapperTests
    {
        [Test]
        public void Roundtrip_PreservesHomeAndDayAnchor()
        {
            var actor = new ActorRecord(
                new ActorId(7), "Villager", ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(21, 22),
                accuracy: 10, dodge: 5, armor: 1, baseDamage: 2,
                home: new GridPosition(3, 4), dayAnchor: new GridPosition(8, 9));

            var back = ActorSaveMapper.ToActor(ActorSaveMapper.ToData(actor));

            Assert.That(back.Home, Is.EqualTo(new GridPosition(3, 4)), "Home must survive the save");
            Assert.That(back.DayAnchor, Is.EqualTo(new GridPosition(8, 9)), "DayAnchor must survive the save");
            Assert.That(back.Position, Is.EqualTo(new GridPosition(21, 22)), "Position stays independent of the anchors");
        }

        // W32 (docs/ruh/w32/01-actor-action-state.md §3.4): the mind block must roundtrip
        // Idle as Idle, a populated state field-by-field, and a corrupt block as Idle.
        [Test]
        public void Roundtrip_IdleActionState_StaysIdle()
        {
            var back = ActorSaveMapper.ToActor(ActorSaveMapper.ToData(Villager()));

            Assert.That(back.ActionState, Is.EqualTo(ActorActionState.Idle));
            Assert.That(back.ActionState.IsIdle, Is.True);
        }

        [Test]
        public void Roundtrip_PopulatedActionState_IsFieldIdentical()
        {
            var actor = Villager();
            var state = ActorActionState.ForIntent(ActorIntent.Eat)
                .Start(ActorActionType.MoveToFood, targetSite: new SiteId(4),
                       targetItem: ItemId.Empty, reservation: new ReservationId(12),
                       startedAtMinutes: 480, policy: ActionInterruptPolicy.NonInterruptible)
                .Advanced()
                .Failed(ActionFailureReason.Unreachable);
            actor.ApplyActionState(state);

            var back = ActorSaveMapper.ToActor(ActorSaveMapper.ToData(actor));

            Assert.That(back.ActionState, Is.EqualTo(state), "every mind field must survive the save");
        }

        // PRD-03: all canonical action families must keep their selected Running phase verbatim.
        // Before this pin, only Eat was representative coverage; a later per-action mapper branch
        // could silently drop Plant/Harvest/Haul/Sleep/Work/Watch/Hunt state.
        [TestCase(ActorIntent.Eat, ActorActionType.ConsumeFood, 0)]
        [TestCase(ActorIntent.Plant, ActorActionType.PlantSeed, 0)]
        [TestCase(ActorIntent.Harvest, ActorActionType.HarvestCrop, 2)]
        [TestCase(ActorIntent.Harvest, ActorActionType.HaulCrop, 2)]
        [TestCase(ActorIntent.Rest, ActorActionType.Sleep, 0)]
        [TestCase(ActorIntent.Work, ActorActionType.PerformWork, 0)]
        [TestCase(ActorIntent.Watch, ActorActionType.OnWatch, 0)]
        [TestCase(ActorIntent.Hunt, ActorActionType.Hunt, 0)]
        public void Roundtrip_SelectedRunningActionFamily_PreservesSemanticState(
            ActorIntent intent,
            ActorActionType action,
            int carriedUnits)
        {
            var actor = Villager();
            var targetSite = action == ActorActionType.Sleep || action == ActorActionType.OnWatch
                ? default(SiteId)
                : new SiteId(4);
            var targetItem = intent == ActorIntent.Eat ? new ItemId(91) : ItemId.Empty;
            var reservation = action == ActorActionType.PerformWork
                || action == ActorActionType.OnWatch
                || action == ActorActionType.Hunt
                    ? ReservationId.Empty
                    : new ReservationId(12);
            var state = ActorActionState.ForIntent(intent)
                .Start(action, targetSite, targetItem, reservation,
                    startedAtMinutes: 480, policy: ActionInterruptPolicy.Interruptible)
                .Advanced()
                .Advanced();
            if (carriedUnits > 0)
                state = state.WithCarriedMatter("wheat", carriedUnits);
            actor.ApplyActionState(state);

            var back = ActorSaveMapper.ToActor(ActorSaveMapper.ToData(actor)).ActionState;

            Assert.That(back, Is.EqualTo(state),
                "phase/progress/targets/reservation/carried matter must survive as one semantic block");
            Assert.That((back.Phase, back.ProgressTicks, back.StartedAtMinutes),
                Is.EqualTo((ActionPhase.Running, 2, 480L)));
            Assert.That((back.TargetItemId, back.TargetSiteId, back.ReservationId, back.CarriedUnits),
                Is.EqualTo((targetItem, targetSite, reservation, carriedUnits)));
            Assert.That(back.CarriedMatterTag, Is.EqualTo(carriedUnits > 0 ? "wheat" : null));
        }

        [Test]
        public void Load_CorruptActionBlock_NormalizesToIdle()
        {
            var outOfRangeEnum = ActorSaveMapper.ToData(Villager());
            outOfRangeEnum.currentIntent = 1;
            outOfRangeEnum.currentAction = 99; // undefined ActorActionType
            outOfRangeEnum.actionPhase = 1;

            var violatedInvariant = ActorSaveMapper.ToData(Villager());
            violatedInvariant.currentAction = 0; // None => all action fields must be zero
            violatedInvariant.actionProgressTicks = 5;

            Assert.That(ActorSaveMapper.ToActor(outOfRangeEnum).ActionState.IsIdle, Is.True,
                "out-of-range enum must reset the whole block to Idle");
            Assert.That(ActorSaveMapper.ToActor(violatedInvariant).ActionState.IsIdle, Is.True,
                "violated None=>all-zero invariant must reset the whole block to Idle");
        }

        private static ActorRecord Villager()
        {
            return new ActorRecord(
                new ActorId(7), "Villager", ActorRole.Talker,
                new EmberStatBlock(10, 10, 10, 10, 10, 10),
                new ActorVitals(new VitalStat(10, 10), new VitalStat(10, 10), new VitalStat(10, 10)),
                new GridPosition(21, 22),
                accuracy: 10, dodge: 5, armor: 1, baseDamage: 2);
        }
    }
}
