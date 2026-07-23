using EmberCrpg.Simulation.AiDm;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.AiDm
{
    /// <summary>M3b.3 pin: the player's voice is a stable function of creation choices.</summary>
    public sealed class PlayerVoiceServiceTests
    {
        [Test]
        public void PlayerVoiceKey_IsStable()
            => Assert.That(PlayerVoiceService.PlayerVoiceKey("Ash-Born", "Warrior"),
                Is.EqualTo(PlayerVoiceService.PlayerVoiceKey("Ash-Born", "Warrior")));

        [Test]
        public void PlayerVoiceKey_ClassChangesTheVoice()
            => Assert.That(PlayerVoiceService.PlayerVoiceKey("Ash-Born", "Warrior"),
                Is.Not.EqualTo(PlayerVoiceService.PlayerVoiceKey("Ash-Born", "Scholar")));

        [Test]
        public void PlayerVoiceKey_NeverZero_EvenOnEmptyInputs()
            => Assert.That(PlayerVoiceService.PlayerVoiceKey(null, null), Is.Not.EqualTo(0UL));
    }
}
