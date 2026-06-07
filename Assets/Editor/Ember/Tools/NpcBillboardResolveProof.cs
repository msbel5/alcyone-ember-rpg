using EmberCrpg.Data.GeneratedAssets;
using EmberCrpg.Presentation.Ember.Sprites;
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

        private static void Trace(string role)
        {
            var id = GeneratedNpcBillboardResolver.BuildFallbackCoreId(role);
            var sprite = GeneratedCoreSpriteLoader.TryLoadPortrait(id);
            Debug.Log(
                "[NpcBillboardResolveProof] role=" + role +
                " file=Assets/Generated/Core/" + id + ".png" +
                " loaded=" + (sprite != null));
        }
    }
}
