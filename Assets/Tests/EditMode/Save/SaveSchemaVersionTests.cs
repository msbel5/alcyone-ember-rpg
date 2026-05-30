using System;
using EmberCrpg.Data.Save;
using EmberCrpg.Simulation.World;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Save
{
    /// <summary>
    /// EMB-012: locks the save-schema-version contract. ToData stamps the current version; ToWorld
    /// accepts equal/older versions (legacy v0 == v1 baseline) and refuses a newer version instead of
    /// silently loading a half-mapped world.
    /// </summary>
    public sealed class SaveSchemaVersionTests
    {
        [Test]
        public void ToData_StampsCurrentSchemaVersion()
        {
            var data = WorldSaveMapper.ToData(new WorldFactory().Create(1337));
            Assert.That(data.schemaVersion, Is.EqualTo(WorldSaveMapper.CurrentSchemaVersion));
        }

        [Test]
        public void ToWorld_AcceptsCurrentVersion_Roundtrip()
        {
            var data = WorldSaveMapper.ToData(new WorldFactory().Create(1337));
            Assert.DoesNotThrow(() => WorldSaveMapper.ToWorld(data, new WorldFactory().Create(1337)));
        }

        [Test]
        public void ToWorld_TreatsLegacyZeroAsV1()
        {
            // A save written before the schemaVersion field existed deserializes it to 0.
            var data = WorldSaveMapper.ToData(new WorldFactory().Create(1337));
            data.schemaVersion = 0;
            Assert.DoesNotThrow(() => WorldSaveMapper.ToWorld(data, new WorldFactory().Create(1337)));
        }

        [Test]
        public void ToWorld_RejectsFutureSchemaVersion()
        {
            var data = WorldSaveMapper.ToData(new WorldFactory().Create(1337));
            data.schemaVersion = WorldSaveMapper.CurrentSchemaVersion + 1;
            Assert.Throws<NotSupportedException>(
                () => WorldSaveMapper.ToWorld(data, new WorldFactory().Create(1337)));
        }
    }
}
