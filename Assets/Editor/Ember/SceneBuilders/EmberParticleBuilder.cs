// Why: AAA scene recipes need atmospheric particles (forge sparks, chimney
// smoke, candle flame, dungeon fog, torch flame). Each method spawns a
// configured ParticleSystem that is cheap (capped emission) and visible without
// requiring artist-authored prefabs.
using UnityEngine;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Builder for the small ambient particle effects every AAA scene needs.
    /// Each method returns the spawned GameObject so the recipe can re-parent
    /// it under a "FocalContent" or "Ambient" container.
    /// </summary>
    public static class EmberParticleBuilder
    {
        /// <summary>Upward-rising sparks for forges, anvils, fire-essence props.</summary>
        public static GameObject AddForgeSparks(string name, Vector3 position, Transform parent = null)
        {
            var go = NewParticleObject(name, position, parent);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.78f, 0.20f, 1f), new Color(1f, 0.45f, 0.10f, 1f));
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 16f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.15f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.85f, 0.30f), 0f), new GradientColorKey(new Color(0.7f, 0.20f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            ps.Play();
            return go;
        }

        /// <summary>Slow drifting gray smoke column for chimneys / forge stacks.</summary>
        public static GameObject AddChimneySmoke(string name, Vector3 position, Transform parent = null)
        {
            var go = NewParticleObject(name, position, parent);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 4.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 1.10f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.30f, 0.27f, 0.24f, 0.40f), new Color(0.45f, 0.40f, 0.36f, 0.32f));
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 6f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.25f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.38f, 0.34f, 0.30f), 0f), new GradientColorKey(new Color(0.18f, 0.17f, 0.16f), 1f) },
                new[] { new GradientAlphaKey(0.50f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            ps.Play();
            return go;
        }

        /// <summary>Small upward orange flame for candles, lanterns, table lights.</summary>
        public static GameObject AddCandleFlame(string name, Vector3 position, Transform parent = null)
        {
            var go = NewParticleObject(name, position, parent);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.30f, 1f), new Color(1f, 0.35f, 0.07f, 1f));
            main.maxParticles = 16;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 24f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.04f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.90f, 0.40f), 0f), new GradientColorKey(new Color(0.85f, 0.20f, 0.06f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            ps.Play();
            return go;
        }

        /// <summary>Wide cold-blue fog volume for dungeons and ruined halls.</summary>
        public static GameObject AddFogVolume(string name, Vector3 position, Transform parent = null, float radius = 6f)
        {
            var go = NewParticleObject(name, position, parent);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.55f, 0.65f, 0.18f), new Color(0.55f, 0.65f, 0.75f, 0.10f));
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 2.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.50f, 0.60f, 0.72f), 0f), new GradientColorKey(new Color(0.40f, 0.48f, 0.58f), 1f) },
                new[] { new GradientAlphaKey(0.18f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            ps.Play();
            return go;
        }

        /// <summary>Tall flickering orange flame for wall torches in dungeons.</summary>
        public static GameObject AddTorchFlame(string name, Vector3 position, Transform parent = null)
        {
            var go = AddCandleFlame(name, position, parent);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
            main.maxParticles = 28;

            var emission = ps.emission;
            emission.rateOverTime = 30f;
            ps.Play();
            return go;
        }

        private static GameObject NewParticleObject(string name, Vector3 position, Transform parent)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.position = position;
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: true);
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop();
            return go;
        }
    }
}
