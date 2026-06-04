// EMB-033: CharacterCreationController portrait generation concern (partial; view is in .Rendering.cs).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Forge;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Domain.Worldgen;
using EmberCrpg.Presentation.Ember.Forge;
using EmberCrpg.Presentation.Ember.Loading;
using EmberCrpg.Presentation.Ember.UI;
using EmberCrpg.Presentation.Ember.Worldgen;
using EmberCrpg.Simulation.CharacterCreation;
using EmberCrpg.Simulation.Forge;
using EmberCrpg.Simulation.Generation;
using EmberCrpg.Ui.Foundation;
using EmberCrpg.Simulation.Worldgen;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public sealed partial class CharacterCreationController
    {
        // LEFT-007: portrait generation is now non-blocking AND always renders something.
        //
        // The blocking LLM Complete() (an 8s sync-over-async HTTP call inside
        // DefaultNpcPortraitJsonProvider.Request) used to run on Unity's main thread here,
        // freezing the game while the Portrait stage opened or a reroll fired. And the result
        // was only ever pushed into a tiny text label — no image was ever built, so the portrait
        // box rendered empty.
        //
        // New flow:
        //  1. SYNCHRONOUSLY publish a deterministic placeholder (NpcPromptJsonDefaults.FromSeed)
        //     into PortraitJson + the visible "portrait" image swatch, immediately. This means a
        //     portrait is on screen the instant the stage opens (never an empty box), and the
        //     synchronous post-condition tests rely on (PortraitJson contains archetype_id) holds
        //     with zero blocking.
        //  2. If a custom provider is injected (tests / non-network sources), call it
        //     synchronously — it is a cheap in-memory delegate, not the blocking network path —
        //     and apply its validated result over the placeholder right away.
        //  3. Otherwise kick the real network LLM call onto a worker thread (Task.Run) and, in
        //     play mode, poll it from a coroutine; when a valid JSON lands we upgrade PortraitJson
        //     + the swatch on the main thread. Empty/invalid results keep the deterministic
        //     placeholder (NpcPromptJsonDefaults.FromSeed). Seed handling is unchanged.
        private void GeneratePortrait()
        {
            // Each generation gets a serial so a late async result from a superseded reroll
            // (or a Lock) cannot clobber the current portrait.
            int generation = ++_portraitGenSerial;
            StopPortraitUpgrade();

            var manifest = GenericNpcBaseManifest.CreateDefault();
            uint portraitSeed = _seed + (uint)(3 - _rerollsRemaining);

            // 1. Deterministic placeholder — visible immediately, no blocking.
            var placeholder = NpcPromptJsonDefaults.FromSeed(portraitSeed, manifest);
            ApplyPortrait(placeholder, "Forging your likeness…", portraitSeed, generation);

            // 2. Injected provider (tests / custom): cheap + synchronous, apply now.
            if (_portraitJsonProvider != null)
            {
                var raw = string.IsNullOrWhiteSpace(_llmJson)
                    ? _portraitJsonProvider(portraitSeed, string.Empty)
                    : _llmJson;
                if (TryUpgradePortrait(raw, manifest, portraitSeed, generation, out var reason))
                    return;

                raw = _portraitJsonProvider(portraitSeed, reason);
                if (!TryUpgradePortrait(raw, manifest, portraitSeed, generation, out reason))
                    AddLog("[portrait] provider invalid twice; deterministic placeholder kept: " + reason + ".");
                return;
            }

            // 3. Default network path: run OFF the main thread, upgrade when it lands (play mode).
            //    A seeded _llmJson (if any) is tried first, still off-thread for the network retry.
            string seededJson = _llmJson;
            var task = ResolvePortraitJsonAsync(portraitSeed, seededJson, manifest);

            if (Application.isPlaying)
                _portraitUpgradeRoutine = StartCoroutine(AwaitPortraitUpgrade(task, manifest, portraitSeed, generation));
            // In edit-mode tests Application.isPlaying is false: we intentionally do NOT block on
            // the task. The deterministic placeholder already satisfies the synchronous contract.
        }

        private static async Task<string> ResolvePortraitJsonAsync(
            uint portraitSeed,
            string seededJson,
            GenericNpcBaseManifest manifest)
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12)))
            {
                var attempt = string.IsNullOrWhiteSpace(seededJson)
                    ? await DefaultNpcPortraitJsonProvider.RequestAsync(portraitSeed, string.Empty, cts.Token)
                        .ConfigureAwait(false)
                    : seededJson;
                if (NpcPromptJsonValidator.TryValidate(attempt, manifest, out _, out var why))
                    return attempt;

                return await DefaultNpcPortraitJsonProvider.RequestAsync(portraitSeed, why, cts.Token)
                    .ConfigureAwait(false);
            }
        }

        // Poll the off-thread LLM task without blocking the main thread; apply a valid result.
        private IEnumerator AwaitPortraitUpgrade(Task<string> task, GenericNpcBaseManifest manifest, uint portraitSeed, int generation)
        {
            while (!task.IsCompleted)
                yield return null;

            _portraitUpgradeRoutine = null;

            // A newer reroll (or Lock) superseded this request — drop the stale result.
            if (generation != _portraitGenSerial)
                yield break;

            string raw = task.Status == TaskStatus.RanToCompletion ? task.Result : string.Empty;
            if (TryUpgradePortrait(raw, manifest, portraitSeed, generation, out var reason))
                AddLog("[portrait] LLM portrait ready.");
            else
                AddLog("[portrait] LLM unavailable/invalid; deterministic placeholder kept: " + reason + ".");
        }

        // Validate raw JSON and, if good, publish it as the portrait (JSON + visible swatch).
        private bool TryUpgradePortrait(string raw, GenericNpcBaseManifest manifest, uint portraitSeed, int generation, out string reason)
        {
            if (NpcPromptJsonValidator.TryValidate(raw, manifest, out var json, out reason))
            {
                ApplyPortrait(json, "Your likeness, drawn from the embers.", portraitSeed, generation);
                return true;
            }
            return false;
        }

        // Single place that writes the portrait: canonical JSON into PortraitJson + the label, and
        // a deterministic visible swatch into the "portrait" image slot with a caption.
        private void ApplyPortrait(NpcPromptJson json, string caption, uint portraitSeed, int generation)
        {
            PortraitJson = json.ToCanonicalJson();
            // Build the texture unconditionally (not gated on _panel) so the redesigned view always has it.
            _characterPortraitTexture = CharacterCreationPortraitSwatch.Build(json);
            if (_panel != null)
            {
                _panel.SetText("portraitJson", PortraitJson);
                if (_step != CreationStep.WorldHistoryReveal)
                    _panel.SetThumbnail("portrait", _characterPortraitTexture);
                _panel.SetVisible("portrait", true);
                _panel.SetText("portraitCaption", caption);
                _panel.SetVisible("portraitCaption", true);
            }

            RefreshPortraitView();
            StartPortraitForgeUpgrade(json, portraitSeed, generation);
        }

        // The redesigned view renders the portrait from _characterPortraitTexture, so a reroll or a late async
        // upgrade must re-render or the Image keeps the stale texture ("portrait didn't refresh"). The legacy
        // _panel updates its thumbnail in place and needs no re-render.
        private void RefreshPortraitView()
        {
            if (_redesignView != null && (_step == CreationStep.Portrait || _step == CreationStep.DossierLaunch))
                Render();
        }

        private void StartPortraitForgeUpgrade(NpcPromptJson json, uint portraitSeed, int generation)
        {
            if (!Application.isPlaying) return;
            if (generation != _portraitGenSerial) return;

            var forge = ForgeLocator.AssetForge;
            if (forge == null) { AddLog("[portrait] forge image: no forge wired."); return; }
            try
            {
                if (!forge.IsAvailable()) { AddLog("[portrait] forge image: forge unavailable (models missing)."); return; }
            }
            catch
            {
                AddLog("[portrait] forge image: IsAvailable() threw.");
                return;
            }

            StopPortraitForgeUpgrade();

            var subject = PortraitPromptBuilder.Build(json);
            var kind = AssetKind.Portrait;
            var spec = new DefaultImageGenSpecFactory().Create(kind, subject, portraitSeed);
            var request = BuildPortraitRequest(kind, spec, generation);

            _portraitForgeCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            Task<AssetGenerationResult> task;
            try
            {
                task = forge.GenerateAsync(request, _portraitForgeCancellation.Token);
            }
            catch (Exception ex)
            {
                _portraitForgeCancellation.Dispose();
                _portraitForgeCancellation = null;
                AddLog("[portrait] Forge request failed to start: " + ex.Message);
                return;
            }

            AddLog("[portrait] forge image: generating " + spec.Width + "x" + spec.Height + "…");
            _portraitForgeUpgradeRoutine = StartCoroutine(AwaitPortraitForgeUpgrade(task, generation));
        }

        private AssetGenerationRequest BuildPortraitRequest(AssetKind kind, ImageGenSpec spec, int generation)
        {
            var style = MoodToStyle(_worldMood);
            var genre = CallingToGenre(_playerCalling);
            var mood = string.IsNullOrWhiteSpace(_worldMood) ? "grim" : _worldMood.Trim();
            var promptHash = PromptHash.Sha256(
                (spec.Prompt ?? string.Empty)
                + "|"
                + (spec.NegativePrompt ?? string.Empty)
                + "|"
                + spec.Width.ToString(CultureInfo.InvariantCulture)
                + "x"
                + spec.Height.ToString(CultureInfo.InvariantCulture)
                + "|"
                + spec.Seed.ToString(CultureInfo.InvariantCulture));

            var requestId = "cc_portrait_"
                + generation.ToString(CultureInfo.InvariantCulture)
                + "_"
                + spec.Seed.ToString(CultureInfo.InvariantCulture);

            return new AssetGenerationRequest(
                requestId,
                kind.ToSubjectKind(),
                style,
                genre,
                mood,
                promptHash,
                spec.Width,
                spec.Height,
                spec.Seed,
                spec.Prompt,
                spec.NegativePrompt,
                steps: spec.Steps);
        }

        private IEnumerator AwaitPortraitForgeUpgrade(Task<AssetGenerationResult> task, int generation)
        {
            while (!task.IsCompleted)
                yield return null;

            _portraitForgeUpgradeRoutine = null;
            if (_portraitForgeCancellation != null)
            {
                _portraitForgeCancellation.Dispose();
                _portraitForgeCancellation = null;
            }

            if (generation != _portraitGenSerial)
                yield break;

            if (task.IsCanceled || task.IsFaulted || task.Status != TaskStatus.RanToCompletion)
            {
                AddLog("[portrait] forge image canceled/faulted: " + (task.Exception?.GetBaseException().Message ?? task.Status.ToString()) + ".");
                yield break;
            }

            var result = task.Result;
            if (!result.Success || result.IsPlaceholder || result.ImageBytes == null || result.ImageBytes.Length == 0)
            {
                AddLog("[portrait] forge image skipped: success=" + result.Success + " placeholder=" + result.IsPlaceholder
                    + " bytes=" + (result.ImageBytes == null ? 0 : result.ImageBytes.Length) + " reason=" + result.FailureReason + ".");
                yield break;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(result.ImageBytes))
            {
                UnityEngine.Object.Destroy(texture);
                yield break;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            _characterPortraitTexture = texture;
            // Don't clobber the continent map the reveal is showing in this shared slot; the dossier restores
            // the portrait from _characterPortraitTexture.
            if (_step != CreationStep.WorldHistoryReveal)
                _panel?.SetThumbnail("portrait", texture);
            RefreshPortraitView();   // re-render the redesigned view so the real forge image actually appears
        }

        private void StopPortraitForgeUpgrade()
        {
            if (_portraitForgeUpgradeRoutine != null)
            {
                StopCoroutine(_portraitForgeUpgradeRoutine);
                _portraitForgeUpgradeRoutine = null;
            }

            if (_portraitForgeCancellation != null)
            {
                try { _portraitForgeCancellation.Cancel(); }
                catch (ObjectDisposedException) { }
                _portraitForgeCancellation.Dispose();
                _portraitForgeCancellation = null;
            }
        }

        // Stop any in-flight upgrade coroutine (used on reroll/lock/regenerate). The Task keeps
        // running to completion harmlessly; its result is discarded via the generation check.
        private void StopPortraitUpgrade()
        {
            if (_portraitUpgradeRoutine != null)
            {
                StopCoroutine(_portraitUpgradeRoutine);
                _portraitUpgradeRoutine = null;
            }

            StopPortraitForgeUpgrade();
        }

        private void AddLog(string line)
        {
            _logLines.Add(line);
            _panel?.LogLine("log", UiLogSeverity.Info, line);
        }

        private string[] BuildAttributeRollSnapshot()
        {
            var rows = new string[StatOrder.Length];
            for (int i = 0; i < StatOrder.Length; i++)
                rows[i] = StatOrder[i] + "=" + SafeStat(_assignedStats, StatOrder[i]);
            return rows;
        }

        private static string ResolveBirthsignId(uint seed)
        {
            var rows = CharacterCreationCatalog.Birthsigns;
            if (rows.Count == 0) return string.Empty;
            return rows[(int)(seed % (uint)rows.Count)].Id;
        }

    }
}
