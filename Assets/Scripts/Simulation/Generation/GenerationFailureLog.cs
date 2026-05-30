using System;
using System.IO;

namespace EmberCrpg.Simulation.Generation
{
    public sealed class GenerationFailureLog
    {
        private readonly string _path;
        private readonly object _lock = new object();

        public GenerationFailureLog(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Failure log path is required.", nameof(path));
            _path = path;
        }

        public string Path => _path;

        public void Append(string entryId, string category, string reason, string exceptionType, string promptHash, long elapsedMs)
        {
            var directory = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            // EMB-039: DateTime.UtcNow is log-only here (a human-readable timestamp on a failure
            // line). It never enters a generated-asset ID or any world/save field — keep it that
            // way so it can't leak into the deterministic identity (docs/DETERMINISM.md).
            var line = "{"
                + "\"ts\":\"" + DateTime.UtcNow.ToString("O") + "\","
                + "\"entryId\":\"" + Escape(entryId) + "\","
                + "\"category\":\"" + Escape(category) + "\","
                + "\"reason\":\"" + Escape(OneLine(reason)) + "\","
                + "\"exceptionType\":\"" + Escape(exceptionType ?? string.Empty) + "\","
                + "\"promptHash\":\"" + Escape(promptHash ?? string.Empty) + "\","
                + "\"elapsedMs\":" + elapsedMs.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "}" + Environment.NewLine;
            lock (_lock) File.AppendAllText(_path, line);
        }

        private static string OneLine(string value)
        {
            return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
