// Polyfill so C# 9 `record` / init-only setters compile on Unity's framework profile (which does not ship
// System.Runtime.CompilerServices.IsExternalInit). Scoped `internal` to the Presentation assembly, where the
// in-game UI mock-data records (IgMockData) live.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
