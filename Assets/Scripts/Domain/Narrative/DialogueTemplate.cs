using System;
using System.Collections.Generic;

namespace EmberCrpg.Domain.Narrative
{
    /// <summary>
    /// Immutable templated text row. Placeholders are simple `{name}` tokens
    /// resolved deterministically against a substitution map. Faz 9 Atom 9.
    /// </summary>
    public sealed class DialogueTemplate
    {
        public DialogueTemplate(string id, string template)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Template id must be non-blank.", nameof(id));
            if (template == null) throw new ArgumentNullException(nameof(template));
            Id = id.Trim();
            Template = template;
        }

        public string Id { get; }
        public string Template { get; }

        public string Render(IReadOnlyDictionary<string, string> substitutions)
        {
            if (substitutions == null) return Template;
            var output = Template;
            foreach (var pair in substitutions)
                output = output.Replace("{" + pair.Key + "}", pair.Value ?? string.Empty);
            return output;
        }
    }

    /// <summary>Deterministic registry of dialogue templates.</summary>
    public sealed class DialogueTemplateRegistry
    {
        private readonly Dictionary<string, DialogueTemplate> _byId = new Dictionary<string, DialogueTemplate>();
        private readonly List<string> _order = new List<string>();

        public int Count => _byId.Count;

        public void Register(DialogueTemplate template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (_byId.ContainsKey(template.Id))
                throw new InvalidOperationException("DialogueTemplateRegistry already contains " + template.Id);
            _byId.Add(template.Id, template);
            _order.Add(template.Id);
        }

        public DialogueTemplate Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _byId.TryGetValue(id.Trim(), out var t) ? t : null;
        }

        public IEnumerable<DialogueTemplate> Templates
        {
            get { foreach (var id in _order) yield return _byId[id]; }
        }
    }
}
