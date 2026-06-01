// LEFT-007 visibility fix: a deterministic, always-visible portrait swatch.
//
// The character-creation Portrait stage used to dump raw NpcPromptJson into a tiny
// text label and never built ANY image, so the "portrait box" rendered empty. This
// baker turns the validated NpcPromptJson (its primary/secondary hue + archetype)
// into a small Texture2D the panel can show via IUiPanel.SetThumbnail. It is fully
// deterministic from the JSON, needs no forge / model / GPU, and runs in well under
// a millisecond on the main thread — so a portrait is visible the instant the stage
// opens, and again whenever a (possibly LLM-upgraded) JSON lands.
//
// This is an intentionally simple "heraldic swatch": a two-hue vertical gradient with
// a darker humanoid silhouette and a warm ember vignette. It is NOT the final forge
// art — when the ONNX portrait forge is wired, it can replace this texture in the same
// "portrait" slot. Until then this guarantees the player never sees an empty box.

using EmberCrpg.Domain.Generation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    internal static class CharacterCreationPortraitSwatch
    {
        private const int Size = 192;

        // Build a deterministic portrait swatch from a validated NpcPromptJson. Never returns
        // null: callers rely on this to always have something visible to show.
        public static Texture2D Build(NpcPromptJson json)
        {
            float primaryHue = Mathf.Repeat((json?.PrimaryHueDegrees ?? 28) / 360f, 1f);
            float secondaryHue = Mathf.Repeat((json?.SecondaryHueDegrees ?? 215) / 360f, 1f);

            // Two warm-leaning anchor colors from the JSON hues; keep saturation/value in the
            // ember-dark-fantasy band so the swatch reads as "portrait", not neon.
            // LIVE-4: the old values (v 0.12-0.62 + a near-black silhouette + a shoulder disc that
            // covered ~84% of the frame) baked a dark muddy blob you could not read as a portrait, and
            // a reroll's new hues were invisible. Brighten the gradient, give the figure a clearly-visible
            // tone, and add a bright rim-light so the bust reads and rerolls obviously differ.
            var top = Color.HSVToRGB(primaryHue, 0.65f, 0.96f);
            var bottom = Color.HSVToRGB(secondaryHue, 0.58f, 0.58f);
            var silhouette = Color.HSVToRGB(secondaryHue, 0.70f, 0.34f); // a visible figure, not near-black
            var rim = Color.HSVToRGB(primaryHue, 0.85f, 0.99f);          // bright rim-light along the figure edge
            var frame = Color.HSVToRGB(Mathf.Repeat(primaryHue + 0.04f, 1f), 0.40f, 0.98f);

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "cc_portrait_swatch",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[Size * Size];
            float cx = Size * 0.5f;
            // Head center sits a little above middle; shoulders fill the bottom.
            float headCx = cx;
            // Unity Texture2D row 0 is the BOTTOM (the gradient `t` above says so) — so the head must sit
            // ABOVE middle and the shoulders fill the BOTTOM. The old values (head 0.42 / shoulder 1.04)
            // were authored as if y=0 were the top, which rendered the bust UPSIDE DOWN.
            float headCy = Size * 0.62f;
            float headR = Size * 0.17f;
            float shoulderCy = Size * -0.04f; // shoulder disc mostly below the frame; only the upper bust shows
            float shoulderR = Size * 0.34f;
            float maxDist = Mathf.Sqrt(cx * cx + cx * cx);

            for (int y = 0; y < Size; y++)
            {
                float t = (float)y / (Size - 1); // 0 = bottom row, 1 = top row in UV space
                var bg = Color.Lerp(bottom, top, t);
                for (int x = 0; x < Size; x++)
                {
                    var c = bg;

                    // Humanoid bust: a head disc + a shoulder disc. The figure uses a clearly-visible tone
                    // (not near-black) with a bright rim-light on its boundary so it reads as a portrait.
                    float hd = Mathf.Sqrt((x - headCx) * (x - headCx) + (y - headCy) * (y - headCy));
                    float sd = Mathf.Sqrt((x - cx) * (x - cx) + (y - shoulderCy) * (y - shoulderCy));
                    float edge = Mathf.Max(headR - hd, shoulderR - sd); // > 0 inside the head OR shoulder
                    if (edge >= 0f)
                    {
                        c = Color.Lerp(silhouette, c, 0.10f);
                        if (edge < 4f) c = Color.Lerp(rim, c, 0.35f); // bright rim on the silhouette boundary
                    }

                    // Soft ember vignette: gently darken the corners for focus without muddying the figure.
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cx) * (y - cx)) / maxDist;
                    float vignette = Mathf.Clamp01(1f - (d * d) * 0.30f);
                    c.r *= vignette; c.g *= vignette; c.b *= vignette;

                    // Gold hairline frame (a few pixels on each edge), matches the UI furniture.
                    if (x < 3 || x >= Size - 3 || y < 3 || y >= Size - 3)
                        c = frame;

                    pixels[y * Size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }
    }
}
