using EmberCrpg.Editor.Ember.Common;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace EmberCrpg.Editor.Ember.SceneBuilders
{
    /// <summary>
    /// Spawns actor and worksite placeholders. Each placeholder is a primitive with a
    /// SpriteRenderer billboard pointing at the camera, so Morrowind-style worlds get an
    /// inhabitable feel before a model pipeline lands. Actor sprites are resolved by name
    /// against <see cref="EmberAssetPaths.CharactersDir"/>.
    /// </summary>
    public static class EmberWorldspaceBuilder
    {
        public static GameObject SpawnActor(
            string actorName,
            string spriteName,
            Vector3 worldPosition,
            Transform parent = null,
            string domainActorKey = null)
        {
            var go = new GameObject(actorName);
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = worldPosition;

            var billboardChild = new GameObject("Billboard");
            billboardChild.transform.SetParent(go.transform, worldPositionStays: false);
            billboardChild.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            var renderer = billboardChild.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSpriteByName(spriteName);
            renderer.sortingOrder = 10;
            FitBillboardToPlayableHeight(billboardChild.transform, renderer, 2.1f);
            AddRuntimeComponent(billboardChild, "EmberCrpg.Presentation.Ember.Views.CameraFacingBillboard");
            var actorView = AddRuntimeComponent(go, "EmberCrpg.Presentation.Ember.Views.ActorView");
            // Codex audit (seventh pass A-P1 #3): serialize the stable domain
            // actor key into the ActorView's _domainActorKey field so the
            // runtime adapter binding is name-independent (and survives
            // GameObject renames in the scene). When a caller does not pass
            // a key, default to the actorName argument — which is what the
            // builder uses as the GameObject name today, so behaviour is
            // preserved for any caller that has not opted in.
            SerializeDomainActorKey(actorView, string.IsNullOrEmpty(domainActorKey) ? actorName : domainActorKey);
            AddInteractable(go, actorName);

            var capsuleShadow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsuleShadow.name = "ShadowProxy";
            capsuleShadow.transform.SetParent(go.transform, worldPositionStays: false);
            capsuleShadow.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            capsuleShadow.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            var sr = capsuleShadow.GetComponent<MeshRenderer>();
            if (sr != null) sr.enabled = false;

            return go;
        }

        public static GameObject SpawnWorksiteMarker(
            string siteName,
            Vector3 worldPosition,
            Transform parent = null,
            Material material = null,
            Vector3? scale = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = siteName;
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = worldPosition;
            go.transform.localScale = scale ?? new Vector3(1.5f, 1.5f, 1.5f);
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material != null ? material : EmberSceneMaterialLibrary.Prop();
            AddRuntimeComponent(go, "EmberCrpg.Presentation.Ember.Views.WorksiteView");
            return go;
        }

        public static GameObject SpawnDecorSprite(
            string decorName,
            string assetPath,
            Vector3 worldPosition,
            float targetHeight,
            Transform parent = null)
        {
            var go = new GameObject(decorName);
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = worldPosition;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSpriteAtPath(assetPath);
            renderer.sortingOrder = 8;
            FitBillboardToPlayableHeight(go.transform, renderer, targetHeight);
            AddRuntimeComponent(go, "EmberCrpg.Presentation.Ember.Views.CameraFacingBillboard");
            return go;
        }

        private static void FitBillboardToPlayableHeight(Transform transform, SpriteRenderer renderer, float targetHeight)
        {
            if (renderer == null || renderer.sprite == null) return;
            var spriteHeight = renderer.sprite.bounds.size.y;
            if (spriteHeight <= 0.001f) return;
            var scale = Mathf.Clamp(targetHeight / spriteHeight, 0.02f, 3f);
            transform.localScale = new Vector3(scale, scale, scale);
        }

        private static Sprite LoadSpriteByName(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            var path = ResolveSpritePath(spriteName);
            return LoadSpriteAtPath(path);
        }

        private static Sprite LoadSpriteAtPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in all)
            {
                if (asset is Sprite s) return s;
            }
            return null;
        }

        private static string ResolveSpritePath(string spriteName)
        {
            var candidate = $"{EmberAssetPaths.CharactersDir}/{spriteName}.png";
            if (IsUsableSprite(candidate)) return candidate;

            var alias = ResolveSpriteAlias(spriteName);
            var aliasPath = $"{EmberAssetPaths.CharactersDir}/{alias}.png";
            return IsUsableSprite(aliasPath) ? aliasPath : candidate;
        }

        private static bool IsUsableSprite(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            return new FileInfo(path).Length >= 4096;
        }

        private static string ResolveSpriteAlias(string spriteName)
        {
            switch (spriteName)
            {
                case "bandit": return "bandit_fixed";
                case "goblin": return "goblin_fixed";
                case "guard": return "knight";
                case "mage": return "sage";
                case "merchant": return "innkeeper";
                case "rogue": return "thief";
                case "warrior": return "blacksmith";
                default: return "blacksmith";
            }
        }

        private static Component AddRuntimeComponent(GameObject host, string fullName)
        {
            var type = ResolveRuntimeType(fullName);
            if (type == null)
            {
                Debug.LogWarning($"Could not resolve runtime component {fullName}");
                return null;
            }

            return host.AddComponent(type);
        }

        private static void SerializeDomainActorKey(Component actorView, string key)
        {
            if (actorView == null || string.IsNullOrEmpty(key)) return;
            // ActorView's _domainActorKey is a [SerializeField] private string.
            // Set it via SerializedObject so the value persists into the
            // generated scene asset.
            var serialized = new SerializedObject(actorView);
            var prop = serialized.FindProperty("_domainActorKey");
            if (prop != null)
            {
                prop.stringValue = key;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static System.Type ResolveRuntimeType(string fullName)
        {
            var qualified = System.Type.GetType(fullName + ", EmberCrpg.Presentation");
            if (qualified != null) return qualified;

            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }

        private static void AddInteractable(GameObject host, string name)
        {
            var type = ResolveRuntimeType("EmberCrpg.Presentation.Ember.Interaction.EmberInteractable");
            if (type == null) return;
            var interactable = host.AddComponent(type);
            // DLG-01 added a 3-arg Setup(string,string,ActorId) overload, so GetMethod("Setup")
            // by name alone now throws AmbiguousMatchException — pin the 2-arg (name, topic) overload.
            var setup = type.GetMethod("Setup", new[] { typeof(string), typeof(string) });
            if (setup != null) setup.Invoke(interactable, new object[] { name, "General" });
        }
    }
}
