using System.Collections.Generic;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Sprites
{
    /// <summary>
    /// ScriptableObject lookup table mapping a string key (item id, actor name, status code)
    /// to a Sprite. Lives in <c>Assets/Art/SpriteRegistries</c>. Used by UI/View scripts to
    /// resolve icons without taking a hard dependency on <c>AssetDatabase</c> at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "Ember/Sprite Registry", fileName = "SpriteRegistry")]
    public sealed class SpriteRegistry : ScriptableObject, EmberCrpg.Presentation.Ember.UI.ISpriteByName
    {
        [System.Serializable]
        public struct Entry
        {
            public string Name;
            public Sprite Sprite;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private Dictionary<string, Sprite> _index;

        public Sprite GetSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (_index == null) Rebuild();
            return _index != null && _index.TryGetValue(name, out var s) ? s : null;
        }

        public void Rebuild()
        {
            _index = new Dictionary<string, Sprite>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Name) || entry.Sprite == null) continue;
                _index[entry.Name] = entry.Sprite;
            }
        }

        private void OnValidate() => _index = null;
    }
}
