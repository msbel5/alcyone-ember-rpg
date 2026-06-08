using EmberCrpg.Presentation.Ember.Sprites;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Bootstrap
{
    public sealed partial class EmberWorldHost
    {
        public Sprite GetSprite(string name)
        {
            var generatedSprite = GeneratedCoreSpriteLoader.TryLoadByName(name);
            if (generatedSprite != null) return generatedSprite;

            return GeneratedCoreSpriteLoader.TryLoadPortrait(name);
        }

        /// <summary>
        /// Audit (eighth pass D-P2): static convenience for UI panels that
        /// don't hold a reference to the host but want to resolve a sprite
        /// by name (e.g. DialogBoxPanel portrait lookup).
        /// </summary>
        public static Sprite GetSpriteFromHost(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var host = Object.FindFirstObjectByType<EmberWorldHost>(FindObjectsInactive.Include);
            return host != null ? host.GetSprite(name) : null;
        }

    }
}
