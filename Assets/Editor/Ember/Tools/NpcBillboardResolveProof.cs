using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Presentation.Ember.Sprites;
using EmberCrpg.Simulation.Generation;
using UnityEditor;
using UnityEngine;

namespace EmberCrpg.Editor.Ember.Tools
{
    public static class NpcBillboardResolveProof
    {
        [MenuItem("Ember/Proof/Trace NPC Billboard Resolve")]
        public static void TraceDefaultRoles()
        {
            Trace("Guard");
            Trace("Sage");
        }

        [MenuItem("Ember/Proof/Trace Generated UI Sprite Resolve")]
        public static void TraceGeneratedUiSprites()
        {
            TraceName("steel_longsword");
            TraceName("fire");
            TraceName("inventory");
        }

        private static void Trace(string role)
        {
            var id = GeneratedNpcBillboardResolver.BuildFallbackCoreId(role);
            var sprite = GeneratedCoreSpriteLoader.TryLoadPortrait(id);
            Debug.Log(
                "[NpcBillboardResolveProof] role=" + role +
                " file=Assets/Generated/Core/" + id + ".png" +
                " loaded=" + (sprite != null));
        }

        private static void TraceName(string name)
        {
            var mapped = GeneratedCoreSpriteNameMapper.TryMap(name, out var id);
            var sprite = GeneratedCoreSpriteLoader.TryLoadByName(name);
            Debug.Log(
                "[GeneratedUiSpriteResolveProof] name=" + name +
                " mapped=" + mapped +
                " file=Assets/Generated/Core/" + id + ".png" +
                " loaded=" + (sprite != null));
        }
    }
}
