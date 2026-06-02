using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.UI.Options
{
    /// <summary>Pattern: registry + reflection discovery. Why: the options host stays closed to modification.</summary>
    public static class OptionsSectionRegistry
    {
        public static IReadOnlyList<IOptionsSection> Discover()
        {
            var sections = new List<IOptionsSection>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in SafeGetTypes(assembly))
                {
                    if (!IsConcreteSection(type) || !TryCreate(type, out var section)) continue;
                    sections.Add(section);
                }
            }

            sections.Sort(CompareSections);
            Debug.Log($"[Options] discovered {sections.Count} sections: {string.Join(", ", sections.Select(x => x.Title))}");
            return sections;
        }

        // Why: IL2CPP/editor assemblies can partially fail type enumeration, but discovery should continue.
        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        // Why: the host only accepts instantiable, parameterless sections.
        private static bool IsConcreteSection(Type type)
        {
            return type != null
                && type.IsClass
                && !type.IsAbstract
                && typeof(IOptionsSection).IsAssignableFrom(type)
                && type.GetConstructor(Type.EmptyTypes) != null;
        }

        // Why: one bad section should not take down the whole menu.
        private static bool TryCreate(Type type, out IOptionsSection section)
        {
            section = null;
            try
            {
                section = Activator.CreateInstance(type) as IOptionsSection;
                return section != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Options] failed to create {type.FullName}: {ex.Message}");
                return false;
            }
        }

        // Why: stable ordering keeps the left-nav deterministic across runs.
        private static int CompareSections(IOptionsSection left, IOptionsSection right)
        {
            int byOrder = left.Order.CompareTo(right.Order);
            return byOrder != 0
                ? byOrder
                : string.Compare(left.Title, right.Title, StringComparison.OrdinalIgnoreCase);
        }
    }
}
