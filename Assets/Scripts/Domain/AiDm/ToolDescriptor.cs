using System;
using System.Collections.Generic;

namespace EmberCrpg.Domain.AiDm
{
    /// <summary>
    /// Data row describing one AI/DM tool: stable id, surface kind, declared
    /// parameter schema, declared output schema, and whether invoking it
    /// mutates world state. Faz 10 Atom 3.
    /// </summary>
    public sealed class ToolDescriptor
    {
        public ToolDescriptor(
            ToolId id,
            ToolSurfaceKind surface,
            IEnumerable<ToolParameter> parameters,
            string outputSchemaKey,
            ToolSideEffect sideEffect)
        {
            if (id.IsEmpty) throw new ArgumentException("ToolDescriptor.Id must be non-empty.", nameof(id));
            if (surface.IsEmpty) throw new ArgumentException("ToolDescriptor.Surface must be set.", nameof(surface));
            if (string.IsNullOrWhiteSpace(outputSchemaKey))
                throw new ArgumentException("ToolDescriptor.OutputSchemaKey must be non-blank.", nameof(outputSchemaKey));

            Id = id;
            Surface = surface;
            Parameters = parameters == null ? new ToolParameter[0] : new List<ToolParameter>(parameters).AsReadOnly();
            OutputSchemaKey = outputSchemaKey;
            SideEffect = sideEffect;
        }

        public ToolId Id { get; }
        public ToolSurfaceKind Surface { get; }
        public IReadOnlyList<ToolParameter> Parameters { get; }
        public string OutputSchemaKey { get; }
        public ToolSideEffect SideEffect { get; }
    }

    /// <summary>
    /// One typed parameter on a <see cref="ToolDescriptor"/>. Schema keys are
    /// stable strings resolved by a registry; no untyped payloads.
    /// </summary>
    public readonly struct ToolParameter
    {
        public ToolParameter(string name, string schemaKey, bool required)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("ToolParameter.Name must be non-blank.", nameof(name));
            if (string.IsNullOrWhiteSpace(schemaKey))
                throw new ArgumentException("ToolParameter.SchemaKey must be non-blank.", nameof(schemaKey));

            Name = name.Trim();
            SchemaKey = schemaKey.Trim();
            Required = required;
        }

        public string Name { get; }
        public string SchemaKey { get; }
        public bool Required { get; }
    }

    /// <summary>Stable side-effect class for a tool: read-only or world mutation.</summary>
    public readonly struct ToolSideEffect : IEquatable<ToolSideEffect>
    {
        private readonly string _code;

        private ToolSideEffect(string code)
        {
            _code = code;
        }

        public static ToolSideEffect Read { get; } = new ToolSideEffect("read");
        public static ToolSideEffect Mutate { get; } = new ToolSideEffect("mutate");

        public string Code => _code ?? string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(_code);

        public bool Equals(ToolSideEffect other) => Code == other.Code;
        public override bool Equals(object obj) => obj is ToolSideEffect other && Equals(other);
        public override int GetHashCode() => Code.GetHashCode();
        public override string ToString() => Code;
        public static bool operator ==(ToolSideEffect a, ToolSideEffect b) => a.Equals(b);
        public static bool operator !=(ToolSideEffect a, ToolSideEffect b) => !a.Equals(b);
    }
}
