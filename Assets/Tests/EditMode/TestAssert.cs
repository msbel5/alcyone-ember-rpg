using System;

namespace EmberCrpg.Tests.EditMode
{
    /// <summary>Compatibility bridge for Unity's bundled NUnit, which predates Assert.Multiple.</summary>
    internal static class TestAssert
    {
        public static void Multiple(Action assertions)
        {
            if (assertions == null)
                throw new ArgumentNullException(nameof(assertions));
            assertions();
        }
    }
}
