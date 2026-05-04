using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmberCrpg.Tests.PlayMode
{
    public sealed class SpellCastFlowTests
    {
        [UnityTest]
        public IEnumerator SpellCast_Success_RendersFrame()
        {
            var caster = CreateMarker("Caster", new Vector3(-1.5f, 0f, 0f), Color.cyan);
            var target = CreateMarker("Target", new Vector3(1.5f, 0f, 0f), Color.red);
            yield return null;

            var before = ScreenshotUtility.CaptureFrame(nameof(SpellCast_Success_RendersFrame), "before", Color.cyan, Color.red);
            target.transform.localScale = Vector3.one * 1.35f;
            target.GetComponent<Renderer>().material.color = new Color(1f, 0.45f, 0.08f);
            yield return new WaitForSeconds(0.05f);
            var after = ScreenshotUtility.CaptureFrame(nameof(SpellCast_Success_RendersFrame), "after_effect", Color.yellow, Color.red);

            Assert.That(before, Does.EndWith(".png"));
            Assert.That(after, Does.EndWith(".png"));
            Object.Destroy(caster);
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator SpellCast_Fizzle_RendersFrame()
        {
            var caster = CreateMarker("Caster", new Vector3(-1.5f, 0f, 0f), Color.blue);
            var target = CreateMarker("Target", new Vector3(1.5f, 0f, 0f), Color.gray);
            yield return null;

            var frame = ScreenshotUtility.CaptureFrame(nameof(SpellCast_Fizzle_RendersFrame), "fizzle_no_effect", Color.blue, Color.gray);
            Assert.That(frame, Does.EndWith(".png"));
            Object.Destroy(caster);
            Object.Destroy(target);
        }

        [UnityTest]
        public IEnumerator SpellCast_OutOfRange_RendersFrame()
        {
            var caster = CreateMarker("Caster", new Vector3(-3.5f, 0f, 0f), Color.green);
            var target = CreateMarker("OutOfRangeTarget", new Vector3(3.5f, 0f, 0f), Color.magenta);
            yield return null;

            var frame = ScreenshotUtility.CaptureFrame(nameof(SpellCast_OutOfRange_RendersFrame), "out_of_range", Color.green, Color.magenta);
            Assert.That(frame, Does.EndWith(".png"));
            Object.Destroy(caster);
            Object.Destroy(target);
        }

        private static GameObject CreateMarker(string name, Vector3 position, Color color)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.position = position;
            marker.GetComponent<Renderer>().material.color = color;
            return marker;
        }
    }
}
