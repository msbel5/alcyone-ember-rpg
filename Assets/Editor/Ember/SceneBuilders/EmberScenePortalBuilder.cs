using UnityEngine;
using UnityEditor;
using EmberCrpg.Presentation.Ember.Interaction;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Builder for scene portals. Creates a gold cube with a world-space label.
    /// </summary>
    public static class EmberScenePortalBuilder
    {
        public static GameObject BuildPortal(Vector3 position, string targetSceneName, string labelText)
        {
            var portal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portal.name = $"Portal_to_{targetSceneName}";
            portal.transform.position = position;
            portal.transform.localScale = new Vector3(1.5f, 2.5f, 0.2f);

            var renderer = portal.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Standard"));
            renderer.sharedMaterial.color = new Color(1f, 0.84f, 0f); // Gold

            var collider = portal.GetComponent<BoxCollider>();
            if (collider != null) collider.isTrigger = true;

            var portalComp = portal.AddComponent<EmberScenePortal>();
            portalComp.SetTarget(targetSceneName);

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(portal.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = labelText;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.1f;
            textMesh.color = Color.yellow;
            // Unity AI audit (P3): newly built TextMesh defaulted to a null
            // Font reference and a null sharedMaterial, producing pink-
            // shader-error labels in Play mode. Assign the LegacyRuntime
            // built-in font + its companion material so the label renders
            // correctly without manual scene authoring.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                textMesh.font = font;
                var labelRenderer = labelObj.GetComponent<MeshRenderer>();
                if (labelRenderer != null && font.material != null)
                    labelRenderer.sharedMaterial = font.material;
            }

            return portal;
        }
    }
}
