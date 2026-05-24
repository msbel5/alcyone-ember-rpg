#if UNITY_EDITOR
using System;
using EmberCrpg.Ui.Foundation;
using NUnit.Framework;
using UnityEngine;

namespace EmberCrpg.Tests.EditMode.Ui
{
    public sealed class UiTokensTests
    {
        [Test]
        public void DefaultTokens_HaveNoClearColors()
        {
            var tokens = ScriptableObject.CreateInstance<UiTokens>();
            try
            {
                Assert.That(tokens.Accent, Is.Not.EqualTo(Color.clear));
                Assert.That(tokens.AccentMuted, Is.Not.EqualTo(Color.clear));
                Assert.That(tokens.Background, Is.Not.EqualTo(Color.clear));
                Assert.That(tokens.Panel, Is.Not.EqualTo(Color.clear));
                Assert.That(tokens.Text, Is.Not.EqualTo(Color.clear));
                Assert.That(tokens.TextMuted, Is.Not.EqualTo(Color.clear));
                Assert.That(tokens.Danger, Is.Not.EqualTo(Color.clear));
                Assert.That(tokens.Warning, Is.Not.EqualTo(Color.clear));
                Assert.That(tokens.Success, Is.Not.EqualTo(Color.clear));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tokens);
            }
        }

        [Test]
        public void SeverityColor_CoversEverySeverity()
        {
            var tokens = ScriptableObject.CreateInstance<UiTokens>();
            try
            {
                foreach (UiLogSeverity severity in Enum.GetValues(typeof(UiLogSeverity)))
                    Assert.That(tokens.SeverityColor(severity), Is.Not.EqualTo(Color.clear), severity.ToString());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tokens);
            }
        }

        [Test]
        public void JsonRoundTrip_PreservesPrimaryValues()
        {
            var tokens = ScriptableObject.CreateInstance<UiTokens>();
            try
            {
                tokens.Accent = new Color(0.2f, 0.3f, 0.4f, 1f);
                tokens.SpacingMd = 19f;
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(tokens), tokens);
                Assert.That(tokens.Accent, Is.EqualTo(new Color(0.2f, 0.3f, 0.4f, 1f)));
                Assert.That(tokens.SpacingMd, Is.EqualTo(19f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tokens);
            }
        }
    }
}
#endif
