using System;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Inventory;
using EmberCrpg.Domain.Worldgen;

namespace EmberCrpg.Simulation.Forge
{
    public static class PromptComposers
    {
        public const string BaseNegative =
            "text, typography, letters, watermark, label, logo, blurry, low contrast, photorealistic, deformed, noisy, cluttered background, frame, border, poster layout, jpeg artifacts, lowres, pixel art, anime, cel shaded, 3d render, unreal engine";

        public const string PortraitNegative =
            "shirtless, bare chest, topless, nude, muscular savage, loincloth, missing armor, missing clothes";

        public static AssetGenerationRequest NpcPortrait(NpcSeedRecord npc, WorldProfile profile)
        {
            if (npc == null) throw new ArgumentNullException(nameof(npc));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var prompt = new PromptTemplate(
                "painted CRPG character portrait, 3/4 view shoulders up, single subject centered, fully clothed with visible gear and armor, dark fantasy oil painting, Gerald Brom + Planescape Torment aesthetic, painterly brushwork, dramatic chiaroscuro lighting, muted tavern backdrop, expressive face, production game portrait, {Style}, {Genre}, mood {Mood}, {NpcName}, {NpcRole}, born {BirthYear}")
                .Interpolate(profile, npc);
            return Build("npc:" + npc.Id.Value, AssetSubjectKind.Npc, profile, prompt, BaseNegative + ", " + PortraitNegative, MixSeed(profile.Seed, npc.Id.Value), 1024, 1024);
        }

        public static AssetGenerationRequest RegionEstablishingShot(RegionRecord region, WorldProfile profile)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var prompt = new PromptTemplate(
                "wide establishing shot for a first-person CRPG region, dark fantasy oil painting, hand-painted terrain, no text, no frame, {Style}, {Genre}, mood {Mood}, {RegionName}, {Biome}")
                .Interpolate(profile, region);
            return Build("region:" + region.Id.Value, AssetSubjectKind.Region, profile, prompt, BaseNegative, MixSeed(profile.Seed, region.Id.Value), 1024, 768);
        }

        public static AssetGenerationRequest ItemIcon(ItemRecord item, WorldProfile profile)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var prompt = new PromptTemplate(
                "A 1024 square painted BG1 CRPG inventory icon, exactly one item centered on pure white background, rich ornate detail, chiaroscuro lighting, painterly Brom oil brushwork, single item only, {Style}, {Genre}, {Material}, {Quality}, {Slot}")
                .Interpolate(profile, item);
            var negative = BaseNegative + ", multiple items, two items, duplicate items, item collection, catalog sheet";
            return Build("item:" + item.Id.Value, AssetSubjectKind.Item, profile, prompt, negative, MixSeed(profile.Seed, item.Id.Value), 1024, 1024);
        }

        public static string CacheKey(AssetGenerationRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return PromptHash.Sha256(request.Prompt + "|" + request.Style + "|" + request.Seed);
        }

        private static AssetGenerationRequest Build(string requestId, AssetSubjectKind subject, WorldProfile profile, string prompt, string negative, uint seed, int width, int height)
        {
            var hash = PromptHash.Sha256(prompt + "|" + profile.Style + "|" + seed);
            return new AssetGenerationRequest(requestId, subject, profile.Style, profile.Genre, profile.MoodKeyword, hash, width, height, seed, prompt, negative);
        }

        private static uint MixSeed(uint seed, ulong id)
        {
            unchecked
            {
                var mixed = seed ^ (uint)id ^ (uint)(id >> 32);
                mixed ^= 0x9E3779B9u;
                mixed *= 16777619u;
                return mixed == 0u ? 1u : mixed;
            }
        }
    }
}
