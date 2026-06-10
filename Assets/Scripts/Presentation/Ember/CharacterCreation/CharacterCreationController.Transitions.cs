// E7-014 (LEFT-020): step routing / gate logic + worldgen-launch transition + history-reveal timing split out of CharacterCreationController.cs (partial, zero behaviour change).
using System;
using System.Collections;
using System.Linq;
using EmberCrpg.Domain.CharacterCreation;
using EmberCrpg.Domain.Configuration;
using EmberCrpg.Domain.Worldgen; // WorldHistoryEvent / WorldHistoryKind for the real pre-dossier reveal
using EmberCrpg.Presentation.Ember.UI; // EmberWorldGenIntent lives here (E7-014 fix: was wrongly .Worldgen)
using EmberCrpg.Presentation.Ember.Worldgen;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public sealed partial class CharacterCreationController
    {
        // Advance to a stage and run its on-enter side effects (question index clamp, stat roll,
        // portrait generation). Kept separate from Continue so Back/jumps reuse the same hooks.
        private void EnterStage(CreationStep next)
        {
            _step = next;
            switch (next)
            {
                case CreationStep.PersonalityQuestions:
                    _questionIndex = Mathf.Clamp(_questionIndex, 0, Mathf.Max(0, _questions.Count - 1));
                    break;
                case CreationStep.StatRolling:
                    if (_activeStats.Count == 0) RollAllAttributes();
                    break;
                case CreationStep.Portrait:
                    GeneratePortrait();
                    break;
                case CreationStep.WorldHistoryReveal:
                    // World GENERATION happens HERE now — after the portrait, before the dossier — so the
                    // world is awakened with the full character in hand (the genesis answers that seed it
                    // were chosen back at steps 2-5). Builds the real-world reveal + starts its streaming.
                    StartPlanetReveal();
                    _historyRevealStartTime = Time.realtimeSinceStartup;
                    _historySkipped = false;
                    if (Application.isPlaying)
                        StartCoroutine(StreamHistoryReveal());
                    break;
            }
        }

        public void SkipHistoryReveal()
        {
            _historySkipped = true;
            Render();
        }

        // The history reveal is a time-based typewriter, but Render() is otherwise only called on input — so
        // without this per-frame pump it never visibly streams (the player just saw a stuck "[history
        // streaming...]"). Redraw each frame until the reveal unlocks (HistoryUnlockSeconds) or the player
        // skips/leaves the step; the per-frame redraw also re-asserts the continent image if the async portrait
        // forge tries to borrow the slot mid-reveal.
        private IEnumerator StreamHistoryReveal()
        {
            while (_step == CreationStep.WorldHistoryReveal && !_historySkipped && !IsHistoryAdvanceUnlocked())
            {
                DrainPlanetReveal();
                Render();
                yield return null;
            }
            if (_step == CreationStep.WorldHistoryReveal)
            {
                DrainPlanetReveal();
                Render();
            }
        }

        public void BeginYourStory()
        {
            if (_step != CreationStep.DossierLaunch && _step != CreationStep.Complete) return;
            _storyLaunched = true;
            AddLog("[launch] You arrive at Ashford with nothing but your wits and a name that means nothing - yet.");
            // Genesis stages 2-4 author the world via WorldMood/PlayerCalling/FateBegins.
            // Those values flow into EmberWorldGenIntent.Mood/Calling/Start, then EmberWorldGenUI
            // hands them to WorldgenAdapter.SeedWorld (see EmberWorldGenUI line ~143). Earlier the
            // first three args were _profileHint / _selectedClassId / _firstSceneName, which silently
            // discarded the player's genesis answers — fixed here so creation actually authors the
            // world. _profileHint stays in the dossier dump; _firstSceneName still drives scene load.
            EmberWorldGenIntent.Pending = new EmberWorldGenIntent(
                _worldMood,
                _playerCalling,
                _fateStart,
                _commanderName,
                _selectedClassId,
                _selectedBirthsignId,
                _selectedAlignmentId,
                _selectedBackgroundId,
                _selectedSkills.OrderBy(v => v).ToArray(),
                BuildAttributeRollSnapshot(),
                _seed,
                _seed,
                _answerChoiceIds.ToArray(),
                PortraitJson);
            // Carry the approved portrait image (real forge face if it landed, else the deterministic
            // swatch) into gameplay so the Character screen shows it instead of the "C" glyph fallback.
            if (!PlayerPortraitHandoff.CopyCurrentToPending() && _characterPortraitTexture != null)
            {
                PlayerPortraitHandoff.Publish(_characterPortraitTexture);
            }
            if (AutoLaunchWorldgen && Application.isPlaying)
                StartCoroutine(BeginVisibleWorldgen());
            Render();
        }

        private bool ComputeCanAdvance()
        {
            switch (_step)
            {
                case CreationStep.CommanderIdentity:
                    return _commanderName.Length >= 2;
                case CreationStep.WorldMood:
                    return !string.IsNullOrWhiteSpace(_worldMood);
                case CreationStep.PlayerCalling:
                    return !string.IsNullOrWhiteSpace(_playerCalling);
                case CreationStep.FateBegins:
                    return !string.IsNullOrWhiteSpace(_fateStart);
                case CreationStep.PersonalityQuestions:
                    return false;
                case CreationStep.WorldHistoryReveal:
                    return IsHistoryAdvanceUnlocked();
                case CreationStep.Birthsign:
                    return !string.IsNullOrWhiteSpace(_selectedBirthsignId);
                case CreationStep.StatRolling:
                    return _rollKept;
                case CreationStep.BuildSelection:
                    // 10/10 playability fix: allow 1-5 skills (auto-fill puts 5 on class select;
                    // user toggle previously trapped at 4). Class+alignment still required.
                    return !string.IsNullOrWhiteSpace(_selectedClassId)
                        && !string.IsNullOrWhiteSpace(_selectedAlignmentId)
                        && _selectedSkills.Count >= 1
                        && _selectedSkills.Count <= 5;
                case CreationStep.Portrait:
                    return true; // portrait is optional-confirm; reroll is available but not required
                case CreationStep.DossierLaunch:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsHistoryAdvanceUnlocked()
        {
            if (_historySkipped) return true;
            if (!_planetRevealBuilt) return false; // don't let the player advance until the world has finished forming
            if (_historyTimeline.Count == 0) return true;
            float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _historyRevealStartTime);
            var options = EmberRuntimeOptionsProvider.Current.CharacterCreation;
            return elapsed >= options.HistoryUnlockSeconds || VisibleHistoryCharacterCount(elapsed) >= FullHistoryText().Length;
        }

        private int VisibleHistoryCharacterCount(float elapsed)
        {
            if (_historySkipped) return FullHistoryText().Length;
            if (_historyTimeline.Count == 0) return 0;

            var options = EmberRuntimeOptionsProvider.Current.CharacterCreation;
            var charsPerSecond = options.HistoryCharsPerSecond;
            var lineDelaySeconds = options.HistoryLineDelaySeconds;
            int visibleCharacters = 0;
            for (int i = 0; i < _historyTimeline.Count; i++)
            {
                float lineStart = i * lineDelaySeconds;
                if (elapsed <= lineStart) break;
                float timeForLine = Mathf.Max(0f, elapsed - lineStart);
                string line = _historyTimeline[i];
                int lineChars = Mathf.Min(line.Length, Mathf.FloorToInt(timeForLine * charsPerSecond));
                visibleCharacters += lineChars;
                if (lineChars >= line.Length && i < _historyTimeline.Count - 1) visibleCharacters += 2;
            }
            return Mathf.Min(visibleCharacters, FullHistoryText().Length);
        }

        private string FullHistoryText()
        {
            return string.Join("\n\n", _historyTimeline);
        }

        private string VisibleHistoryText()
        {
            string full = FullHistoryText();
            if (_historySkipped) return full;

            float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _historyRevealStartTime);
            int count = VisibleHistoryCharacterCount(elapsed);
            if (count <= 0) return string.Empty;
            if (count >= full.Length) return full;
            return full.Substring(0, count);
        }

        // "The World Awakens" (pre-dossier): build the history reveal from the REAL deterministic world the
        // player is about to inhabit, not a templated fake. Runs the SAME genesis-answers -> WorldgenService
        // path the gameplay scene uses, with the SAME seed, so the centuries of geography + civilisation shown
        // here ARE the world that loads after the dossier. Falls back to the templated reveal if generation
        // fails for any reason (the reveal must never block creation).
        // Configured era span (years) of the previewed world — stamped by TryGenerateWorldPreview from
        // WorldgenParameters.HistoryYears so the reveal shows a STABLE span, not a per-seed event delta.
        private int _previewEraYears = 400;

        // The rendered continent (the SAME overland map the M screen shows) for the "World Awakens" reveal so
        // the player SEES the world they shaped; + the character portrait texture, retained so the dossier can
        // restore the image slot after the reveal borrowed it for the map.
        private Texture2D _revealMapTexture;
        private Texture2D _characterPortraitTexture;

        // Start building the player's actual PLANET off the main thread, streaming each geological stage into
        // the reveal as it forms (the "World Awakens" live feed). The SAME planet is cached in
        // PlanetWorldContext, so SeedWorld reuses it (no second generation) and reveal == the world you play.
        private void StartPlanetReveal()
        {
            _historyTimeline.Clear();
            _planetRevealBuilt = false;
            _revealMapTexture = null;
            _historyTimeline.Add("From the planet's ember, a world begins to take shape...");
            try
            {
                var style = WorldGenesisMapper.ToStyle(_worldMood);
                var genre = WorldGenesisMapper.ToGenre(_worldMood, _playerCalling, _fateStart);
                var parameters = EmberCrpg.Simulation.Worldgen.WorldgenParameters.For(style, genre);
                _previewEraYears = parameters.HistoryYears;
                _planetObserver = new EmberCrpg.Presentation.Ember.Worldgen.StreamingPlanetObserver();
                uint seed = _seed;
                _planetGenTask = System.Threading.Tasks.Task.Run(() =>
                    EmberCrpg.Presentation.Ember.Worldgen.PlanetWorldService.GetOrGenerate(seed, parameters, _planetObserver));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[charcreation] planet reveal start failed; templated reveal. " + ex.Message);
                BuildTemplatedHistoryTimeline();
                _planetRevealBuilt = true;
            }
        }

        // Each frame: drain the streamed stage lines into the timeline; once the off-thread generation finishes,
        // append the history chronicle + render the actual planet. Degrades to the templated reveal on failure.
        private void DrainPlanetReveal()
        {
            if (_planetObserver != null)
            {
                while (_planetObserver.TryDequeue(out var line))
                    _historyTimeline.Add(line);
            }

            if (_planetRevealBuilt || _planetGenTask == null || !_planetGenTask.IsCompleted)
                return;

            _planetRevealBuilt = true;
            EmberCrpg.Simulation.Worldgen.GeneratedWorld world =
                _planetGenTask.Status == System.Threading.Tasks.TaskStatus.RanToCompletion ? _planetGenTask.Result : null;
            if (world == null || world.History == null || world.History.Count == 0)
            {
                BuildTemplatedHistoryTimeline();
                return;
            }

            _historyTimeline.Add("The world awakens - " + world.Settlements.Count + " settlements across "
                + world.Regions.Count + " regions, across " + world.History.Count + " recorded years of history.");
            var notable = SelectNotableHistory(world.History, 24);
            for (int i = 0; i < notable.Count; i++)
            {
                var e = notable[i];
                string subject = string.IsNullOrWhiteSpace(e.Subject) ? string.Empty : e.Subject + " - ";
                _historyTimeline.Add("Year " + e.Year + " - " + subject + e.Detail);
            }
            _revealMapTexture = TryBuildRevealMapTexture(world);
        }

        // Generate the player's actual world for the reveal. Pure + deterministic (no hydration / no sim side
        // effects): mirrors DomainSimulationAdapter.SeedWorld's WorldGenesisMapper -> WorldgenParameters ->
        // WorldgenService.Generate path with the SAME _seed, so reveal == played world.
        private EmberCrpg.Simulation.Worldgen.GeneratedWorld TryGenerateWorldPreview()
        {
            try
            {
                var style = WorldGenesisMapper.ToStyle(_worldMood);
                var genre = WorldGenesisMapper.ToGenre(_worldMood, _playerCalling, _fateStart);
                var parameters = EmberCrpg.Simulation.Worldgen.WorldgenParameters.For(style, genre);
                _previewEraYears = parameters.HistoryYears;
                return EmberCrpg.Simulation.Worldgen.WorldgenService.Generate(_seed, parameters);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[charcreation] world preview generation failed; using templated reveal. " + ex.Message);
                return null;
            }
        }

        // Render the generated continent (the SAME overland map the M screen shows) to a texture for the
        // "World Awakens" reveal, so the player watches their world appear before the dossier. Pure read of
        // the already-generated world; failures degrade to no image (the chronicle text still streams).
        private Texture2D TryBuildRevealMapTexture(EmberCrpg.Simulation.Worldgen.GeneratedWorld world)
        {
            try
            {
                var geography = world?.Geography;
                if (geography == null)
                    throw new InvalidOperationException("Generated world has no geography.");

                var parameters = new EmberCrpg.Domain.Overland.OverlandParameters(
                    geography.Width,
                    geography.Height);
                var map = EmberCrpg.Simulation.Overland.OverlandWorldgen.Generate(
                    world,
                    parameters);
                // ONE map truth: the reveal must show the SAME render the in-game map uses. The playtest
                // caught the two disagreeing by a vertical mirror — the legacy raster path draws the world
                // flipped relative to the planet atlas. Planet first; raster only for legacy worlds.
                byte[] revealRgba;
                int revealW, revealH;
                if (EmberCrpg.Simulation.Overland.PlanetAtlas.TryRender(map, 1024, 512, out var planetImage))
                {
                    revealRgba = planetImage.Rgba;
                    revealW = planetImage.Width;
                    revealH = planetImage.Height;
                }
                else
                {
                    var image = EmberCrpg.Simulation.Overland.OverlandMapImageSampler.Sample(map, 1024, 512);
                    revealRgba = image.RgbaBytes;
                    revealW = image.Width;
                    revealH = image.Height;
                }

                var texture = new Texture2D(revealW, revealH, TextureFormat.RGBA32, mipChain: false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    // Bilinear: the reveal map is a painting of the world, not a tile inspector — Point made
                    // the 1024x512 relief render look like chunky pixels ("the maps got uglier").
                    filterMode = FilterMode.Bilinear,
                };
                texture.LoadRawTextureData(revealRgba);
                texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[charcreation] reveal map render failed; chronicle text only. " + ex.Message);
                return null;
            }
        }

        // Pick a readable, chronological arc from the (thousands of) generated events: prefer high-signal kinds
        // (life, foundings, migrations, wars, alliances, crownings); if too few, sample all events. Even sampling
        // across the list spans the genesis -> migration -> civilisation -> present arc the designer wants.
        private static System.Collections.Generic.List<WorldHistoryEvent> SelectNotableHistory(
            System.Collections.Generic.IReadOnlyList<WorldHistoryEvent> history, int max)
        {
            var notableKinds = new System.Collections.Generic.HashSet<WorldHistoryKind>
            {
                WorldHistoryKind.LifeEmerged, WorldHistoryKind.CivilizationFounded,
                WorldHistoryKind.SettlementFounded, WorldHistoryKind.MigrationWave,
                WorldHistoryKind.WarDeclared, WorldHistoryKind.BattleFought,
                WorldHistoryKind.SiteSacked, WorldHistoryKind.CivilizationDestroyed,
                WorldHistoryKind.FactionAlliance, WorldHistoryKind.LeaderCrowned,
            };
            var filtered = new System.Collections.Generic.List<WorldHistoryEvent>();
            for (int i = 0; i < history.Count; i++)
                if (notableKinds.Contains(history[i].Kind)) filtered.Add(history[i]);

            var source = filtered.Count >= max
                ? filtered
                : new System.Collections.Generic.List<WorldHistoryEvent>(history);
            if (source.Count <= max)
            {
                source.Sort(CompareHistoryEvents);
                return source;
            }

            var result = new System.Collections.Generic.List<WorldHistoryEvent>(max);
            for (int i = 0; i < max; i++)
            {
                int idx = (int)((long)i * (source.Count - 1) / (max - 1));
                result.Add(source[idx]);
            }
            result.Sort(CompareHistoryEvents);
            return result;
        }

        private static int CompareHistoryEvents(WorldHistoryEvent left, WorldHistoryEvent right)
        {
            int byYear = left.Year.CompareTo(right.Year);
            if (byYear != 0) return byYear;
            int byKind = left.Kind.CompareTo(right.Kind);
            if (byKind != 0) return byKind;
            int bySubject = string.CompareOrdinal(left.Subject ?? string.Empty, right.Subject ?? string.Empty);
            if (bySubject != 0) return bySubject;
            return string.CompareOrdinal(left.Detail ?? string.Empty, right.Detail ?? string.Empty);
        }

        // Templated fallback reveal (used only if the real generation above fails). Original creation flow.
        private void BuildTemplatedHistoryTimeline()
        {
            _historyTimeline.Clear();
            var score = _creationService.ScoreAnswers(_answerChoiceIds);
            string strongestClass = score.OrderByDescending(p => p.Value).FirstOrDefault().Key ?? "wanderer";
            int strongestWeight = score.TryGetValue(strongestClass, out var weight) ? weight : 0;

            int startYear = 118;
            int span = 1200;
            int events = 30;
            int stepYears = span / events;
            for (int i = 0; i < events; i++)
            {
                int year = startYear + (i * stepYears);
                string headline = HistoricalHeadline(i, strongestClass, strongestWeight);
                string summary = HistoricalSummary(i, strongestClass);
                string tags = HistoricalTags(i, strongestClass);
                _historyTimeline.Add("Year " + year + " - " + headline + "\n" + summary + "\n" + tags);
            }
        }

        private static string HistoricalHeadline(int index, string classId, int weight)
        {
            string[] templates =
            {
                "The Ember Court drafts a brittle treaty",
                "A forgotten observatory reopens under stormlight",
                "Three trade roads collapse into one guarded pass",
                "The old river changes course without warning",
                "The frontier bells ring before dawn",
                "A masked council claims the archive vaults",
            };
            return templates[index % templates.Length] + " (" + classId + " " + Mathf.Max(1, weight) + ")";
        }

        private static string HistoricalSummary(int index, string classId)
        {
            string[] moods = { "solemn", "defiant", "hungry", "watchful", "fervent", "fractured" };
            return "Witnesses describe a " + moods[index % moods.Length] + " age where " + classId + " instincts shaped every oath.";
        }

        private static string HistoricalTags(int index, string classId)
        {
            string[] tags = { "[trade]", "[faith]", "[war]", "[arcana]", "[law]", "[survival]" };
            return tags[index % tags.Length] + " [importance:" + ((index % 5) + 1) + "] [facet:" + classId + "]";
        }
    }
}
