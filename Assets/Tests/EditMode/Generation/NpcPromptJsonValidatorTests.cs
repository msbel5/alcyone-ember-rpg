using EmberCrpg.Domain.Generation;
using EmberCrpg.Simulation.Generation;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.Generation
{
    public sealed class NpcPromptJsonValidatorTests
    {
        private const string ValidJson = "{\"archetype_id\":\"humanoid_male\",\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[\"wary\",\"soot-stained\"],\"distinctive_features\":[\"scar\",\"iron earring\"],\"clothing_style\":\"leather jerkin\",\"accessory\":\"talisman\",\"world_style_anchor\":\"ember-warm\"}";

        [Test]
        public void ValidJsonAccepts()
        {
            Assert.That(NpcPromptJsonValidator.TryValidate(ValidJson, GenericNpcBaseManifest.CreateDefault(), out var value, out var reason), Is.True, reason);
            Assert.That(value.ArchetypeId, Is.EqualTo("humanoid_male"));
        }

        [TestCase("{\"archetype_id\":\"dragon\",\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[],\"distinctive_features\":[],\"clothing_style\":\"leather\",\"accessory\":\"ring\",\"world_style_anchor\":\"ember-warm\"}", "unknown_archetype")]
        [TestCase("{\"archetype_id\":\"humanoid_male\",\"primary_hue_degrees\":360,\"secondary_hue_degrees\":215,\"mood_keywords\":[],\"distinctive_features\":[],\"clothing_style\":\"leather\",\"accessory\":\"ring\",\"world_style_anchor\":\"ember-warm\"}", "hue_out_of_range")]
        [TestCase("{\"archetype_id\":\"humanoid_male\",\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[\"a\",\"b\",\"c\",\"d\",\"e\",\"f\"],\"distinctive_features\":[],\"clothing_style\":\"leather\",\"accessory\":\"ring\",\"world_style_anchor\":\"ember-warm\"}", "array_too_long")]
        [TestCase("{\"archetype_id\":\"humanoid_male\",\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[],\"distinctive_features\":[],\"clothing_style\":\"abcdefghijklmnopqrstuvwxyzabcdefghijklmno\",\"accessory\":\"ring\",\"world_style_anchor\":\"ember-warm\"}", "string_too_long")]
        [TestCase("{\"archetype_id\":\"humanoid_male\",\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[\"wary\"],\"distinctive_features\":[\"caf\\u00e9\"],\"clothing_style\":\"leather\",\"accessory\":\"ring\",\"world_style_anchor\":\"ember-warm\"}", "non_ascii")]
        [TestCase("{\"archetype_id\":\"humanoid_male\",\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[],\"distinctive_features\":[],\"clothing_style\":\"leather\",\"accessory\":\"ring\",\"world_style_anchor\":\"ember-warm\",\"foo\":1}", "unknown_field:foo")]
        [TestCase("{\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[],\"distinctive_features\":[],\"clothing_style\":\"leather\",\"accessory\":\"ring\",\"world_style_anchor\":\"ember-warm\"}", "missing_field:archetype_id")]
        [TestCase("{\"archetype_id\":\"humanoid_male\",\"primary_hue_degrees\":28,\"secondary_hue_degrees\":215,\"mood_keywords\":[],\"distinctive_features\":[],\"clothing_style\":\"   \",\"accessory\":\"ring\",\"world_style_anchor\":\"ember-warm\"}", "empty_string")]
        public void InvalidJsonRejectsWithReason(string json, string expectedReason)
        {
            Assert.That(NpcPromptJsonValidator.TryValidate(json, GenericNpcBaseManifest.CreateDefault(), out _, out var reason), Is.False);
            Assert.That(reason, Is.EqualTo(expectedReason));
        }

        [Test]
        public void FallbackIsDeterministic()
        {
            var a = NpcPromptJsonDefaults.FromSeed(42u, GenericNpcBaseManifest.CreateDefault());
            var b = NpcPromptJsonDefaults.FromSeed(42u, GenericNpcBaseManifest.CreateDefault());
            Assert.That(a.ToCanonicalJson(), Is.EqualTo(b.ToCanonicalJson()));
        }
    }
}
