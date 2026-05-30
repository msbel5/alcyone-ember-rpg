using System;
using EmberCrpg.Domain.Core;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    /// <summary>Verifies the Phase 5 slow world-process primitive.</summary>
    public sealed class WorldProcessDefinitionTests
    {
        [Test]
        public void Definition_StoresProcessData()
        {
            var def = CreateDef();

            Assert.That(def.Id, Is.EqualTo(new WorldProcessId("grow_wheat_sprout")));
            Assert.That(def.DisplayName, Is.EqualTo("Grow Wheat Sprout"));
            Assert.That(def.DurationDays, Is.EqualTo(3));
            Assert.That(def.OutputEventReason, Is.EqualTo("plant_stage_advanced"));
        }

        [Test]
        public void Instance_AdvancesUntilCompleteWithoutOvershoot()
        {
            var instance = new WorldProcessInstance(new WorldComponentId(50), CreateDef(), new SiteId(2), new WorldComponentId(90), 0);

            instance = instance.AdvanceOneDay().AdvanceOneDay();
            Assert.That(instance.ElapsedDays, Is.EqualTo(2));
            Assert.That(instance.RemainingDays, Is.EqualTo(1));
            Assert.That(instance.IsComplete, Is.False);

            instance = instance.AdvanceOneDay();
            Assert.That(instance.IsComplete, Is.True);
            Assert.That(instance.AdvanceOneDay(), Is.SameAs(instance));
        }

        [Test]
        public void Constructors_RejectInvalidRows()
        {
            Assert.Throws<ArgumentException>(() => new WorldProcessId(" "));
            Assert.Throws<ArgumentException>(() => new WorldProcessDef(default, "Grow", 1, "done"));
            Assert.Throws<ArgumentException>(() => new WorldProcessDef(new WorldProcessId("grow"), " ", 1, "done"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldProcessDef(new WorldProcessId("grow"), "Grow", 0, "done"));
            Assert.Throws<ArgumentException>(() => new WorldProcessDef(new WorldProcessId("grow"), "Grow", 1, " "));
            Assert.Throws<ArgumentException>(() => new WorldProcessInstance(default, CreateDef(), new SiteId(2), new WorldComponentId(90), 0));
            Assert.Throws<ArgumentNullException>(() => new WorldProcessInstance(new WorldComponentId(50), null, new SiteId(2), new WorldComponentId(90), 0));
            Assert.Throws<ArgumentException>(() => new WorldProcessInstance(new WorldComponentId(50), CreateDef(), default, new WorldComponentId(90), 0));
            Assert.Throws<ArgumentException>(() => new WorldProcessInstance(new WorldComponentId(50), CreateDef(), new SiteId(2), default, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldProcessInstance(new WorldComponentId(50), CreateDef(), new SiteId(2), new WorldComponentId(90), 4));
        }

        private static WorldProcessDef CreateDef()
        {
            return new WorldProcessDef(
                new WorldProcessId("grow_wheat_sprout"),
                "Grow Wheat Sprout",
                3,
                "plant_stage_advanced");
        }
    }
}
