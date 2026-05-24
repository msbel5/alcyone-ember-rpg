using System.Collections.Generic;
using EmberCrpg.Domain.Generation;
using EmberCrpg.Simulation.CharacterCreation;
using EmberCrpg.Simulation.Generation;
using EmberCrpg.Ui.Foundation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.CharacterCreation
{
    public sealed class CharacterCreationController : MonoBehaviour
    {
        private readonly List<string> _skills = new List<string>();
        private readonly List<string> _logLines = new List<string>();
        private uint _seed;
        private string _llmJson;
        private int _step;
        private int _rerollsRemaining = 3;
        private IUiPanel _panel;

        public IReadOnlyList<string> LogLines => _logLines;
        public bool CanAdvance => _step == 0 ? _skills.Count == 3 : true;
        public string PortraitJson { get; private set; } = string.Empty;
        public bool CanRerollPortrait => _rerollsRemaining > 0;

        public static CharacterCreationController CreateForTests(uint seed, string llmJson)
        {
            var go = new GameObject("CharacterCreationControllerTest");
            var controller = go.AddComponent<CharacterCreationController>();
            controller.Configure(seed, llmJson);
            return controller;
        }

        public void Configure(uint seed, string llmJson)
        {
            _seed = seed;
            _llmJson = llmJson ?? string.Empty;
            _panel = UiSurfaceLocator.Current?.Mount("CharacterCreation");
            _panel?.SetText("step", "skills");
        }

        public void SelectSkill(string skill)
        {
            if (string.IsNullOrWhiteSpace(skill)) return;
            if (_skills.Contains(skill)) _skills.Remove(skill);
            else if (_skills.Count < 3) _skills.Add(skill);
            _panel?.SetText("skills", string.Join(", ", _skills));
        }

        public void Continue()
        {
            if (!CanAdvance) return;
            if (_step == 0)
            {
                AddLog("[choice] Picked: " + string.Join(", ", _skills) + ".");
                _step = 1;
                _panel?.SetText("step", "attributes");
                return;
            }
            if (_step == 2)
            {
                GeneratePortrait();
                _step = 3;
                _panel?.SetText("step", "portrait");
            }
        }

        public List<AttributeRoll> RollAllAttributes()
        {
            var rolls = new List<AttributeRoll>();
            var attrs = new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" };
            for (int i = 0; i < attrs.Length; i++)
            {
                var roll = AttributeRoller.Roll4d6DropLowest(_seed + (uint)i, attrs[i]);
                rolls.Add(roll);
                AddLog(roll.LogLine);
            }
            _step = 2;
            return rolls;
        }

        public void ChooseBackground(string background)
        {
            AddLog("[choice] Background: " + background + ".");
        }

        public void RerollPortrait()
        {
            if (_rerollsRemaining <= 0) return;
            _rerollsRemaining--;
            GeneratePortrait();
        }

        public void LockPortrait()
        {
            _rerollsRemaining = 0;
        }

        private void GeneratePortrait()
        {
            if (!NpcPromptJsonValidator.TryValidate(_llmJson, GenericNpcBaseManifest.CreateDefault(), out var json, out _))
                json = NpcPromptJsonDefaults.FromSeed(_seed + (uint)(3 - _rerollsRemaining), GenericNpcBaseManifest.CreateDefault());
            PortraitJson = json.ToCanonicalJson();
            _panel?.SetText("portraitJson", PortraitJson);
            AddLog("[portrait] JSON ready.");
        }

        private void AddLog(string line)
        {
            _logLines.Add(line);
            _panel?.LogLine("log", UiLogSeverity.Info, line);
        }
    }
}
