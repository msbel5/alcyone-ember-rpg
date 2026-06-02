using System;
using EmberCrpg.Domain.Process;
using EmberCrpg.Domain.Worldgen;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Process
{
    public sealed class NpcRoleJobMapperTests
    {
        [Test]
        public void ToJobKind_CoversEveryNpcRole()
        {
            foreach (NpcRole role in Enum.GetValues(typeof(NpcRole)))
                Assert.That(NpcRoleJobMapper.ToJobKind(role), Is.EqualTo(Expected(role)), role.ToString());
        }

        private static JobKind? Expected(NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Blacksmith:
                    return JobKind.Smith;
                case NpcRole.Farmer:
                    return JobKind.Farmer;
                default:
                    return null;
            }
        }
    }
}
