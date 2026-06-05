using EmberCrpg.Data.GeneratedAssets;
using NUnit.Framework;

namespace EmberCrpg.Tests.EditMode.GeneratedAssets
{
    public sealed class GeneratedAssetSpritePipelineTests
    {
        [Test]
        public void PromptBuilder_ExpandsPlaceholdersDeterministically()
        {
            var record = NewRecord();
            var preset = GeneratedAssetPromptPreset.CreateRuntimeInstance();
            preset.presetName = "npc";
            preset.positiveTemplate = "one {role} from {biome}";
            preset.negativeTemplate = "no {material}";

            var first = GeneratedAssetPromptBuilder.BuildJob(record, preset, 512, 768, 1, 0f, "euler", "normal", "sdxl");
            var second = GeneratedAssetPromptBuilder.BuildJob(record, preset, 512, 768, 1, 0f, "euler", "normal", "sdxl");

            Assert.That(first.positivePrompt, Is.EqualTo("one scout from marsh"));
            Assert.That(first.negativePrompt, Is.EqualTo("no linen"));
            Assert.That(first.jobId, Is.EqualTo(second.jobId));
        }

        [Test]
        public void PromptBuilder_MissingPlaceholders_DoesNotCrash()
        {
            var record = NewRecord();
            var preset = GeneratedAssetPromptPreset.CreateRuntimeInstance();
            preset.positiveTemplate = "one {missing}";
            preset.negativeTemplate = string.Empty;

            var job = GeneratedAssetPromptBuilder.BuildJob(record, preset, 512, 768, 1, 0f, "euler", "normal", "sdxl");

            Assert.That(job.positivePrompt, Is.EqualTo("one {missing}"));
            Assert.That(job.negativePrompt, Is.EqualTo(string.Empty));
        }

        [Test]
        public void AlphaAnalyzer_FindsLargestComponentBounds()
        {
            var alpha = new byte[64];
            Fill(alpha, 8, 2, 2, 3, 4, 255);
            Fill(alpha, 8, 6, 6, 1, 1, 255);

            var analysis = GeneratedSpriteAlphaAnalyzer.Analyze(8, 8, alpha, 1, 2);

            Assert.That(analysis.mainBounds.x, Is.EqualTo(2));
            Assert.That(analysis.mainBounds.y, Is.EqualTo(2));
            Assert.That(analysis.mainBounds.width, Is.EqualTo(3));
            Assert.That(analysis.mainBounds.height, Is.EqualTo(4));
        }

        [Test]
        public void AlphaAnalyzer_WarnsWhenTwoLargeComponentsExist()
        {
            var alpha = new byte[100];
            Fill(alpha, 10, 1, 1, 3, 3, 255);
            Fill(alpha, 10, 6, 1, 3, 3, 255);

            var analysis = GeneratedSpriteAlphaAnalyzer.Analyze(10, 10, alpha, 1, 4);

            Assert.That(analysis.largeComponentCount, Is.EqualTo(2));
            Assert.That(analysis.warnings, Does.Contain("multiple_large_components"));
        }

        private static GeneratedAssetRecord NewRecord()
        {
            var record = new GeneratedAssetRecord
            {
                kind = GeneratedAssetKind.CharacterBillboard,
                seed = 42,
                displayName = "Scout",
            };
            record.key.archetype = "ranger";
            record.key.biome = "marsh";
            record.key.role = "scout";
            record.key.material = "linen";
            record.key.styleVersion = "v1";
            record.SyncIdentity();
            return record;
        }

        private static void Fill(byte[] alpha, int width, int x, int y, int w, int h, byte value)
        {
            for (var yy = y; yy < y + h; yy++)
            for (var xx = x; xx < x + w; xx++)
                alpha[(yy * width) + xx] = value;
        }
    }
}
