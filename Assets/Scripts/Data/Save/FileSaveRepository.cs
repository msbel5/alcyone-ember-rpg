// Why this file is intentionally long: FileSaveRepository owns all pure file-slot atomicity, quarantine, metadata, and legacy compatibility behavior in one deterministic boundary.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EmberCrpg.Data.Save
{
    /// <summary>
    /// EMB-011: durable file-based save slots. Replaces the single PlayerPrefs blob with
    /// per-slot JSON files under &lt;root&gt;/saves/slot_{n}.json, plus corrupt-save quarantine.
    /// Pure C# (System.IO only) so it is unit-testable without Unity; the Presentation save service
    /// injects Application.persistentDataPath as the root. PlayerPrefs is kept by the caller ONLY as
    /// a "last slot" pointer, not as the store.
    /// </summary>
    public sealed class FileSaveRepository
    {
        private const int DefaultManualCap = 10;
        private const int DefaultMetadataVersion = 1;
        private static readonly Regex JsonStringFieldPatternTemplate = new Regex("\"{0}\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"", RegexOptions.Compiled);
        private static readonly Regex JsonIntFieldPatternTemplate = new Regex("\"{0}\"\\s*:\\s*(-?\\d+)", RegexOptions.Compiled);
        private readonly string _savesDir;

        public FileSaveRepository(string rootDir)
        {
            if (string.IsNullOrWhiteSpace(rootDir)) throw new ArgumentException("rootDir required", nameof(rootDir));
            _savesDir = Path.Combine(rootDir, "saves");
        }

        public int ManualCapDefault => DefaultManualCap;

        public string SlotPath(int slot) => Path.Combine(_savesDir, "slot_" + slot + ".json");

        public string SlotPath(SaveSlotId id) => Path.Combine(_savesDir, id.FileStem() + ".json");

        public string MetadataPath(SaveSlotId id) => Path.Combine(_savesDir, id.FileStem() + ".meta.json");

        /// <summary>Write the save JSON for a slot (creates the saves dir). Atomic-ish: writes a
        /// .tmp then moves into place so a crash mid-write can't corrupt an existing slot.</summary>
        public void Save(int slot, string json)
        {
            Directory.CreateDirectory(_savesDir);
            WriteAtomically(SlotPath(slot), json ?? string.Empty);
        }

        /// <summary>Write payload+sidecar for a named slot. Payload publish is authoritative; sidecar
        /// write is best-effort so metadata failure cannot erase a valid payload slot.</summary>
        public void Save(SaveSlotId id, string payloadJson, SaveSlotMetadata meta)
        {
            Directory.CreateDirectory(_savesDir);
            WriteAtomically(SlotPath(id), payloadJson ?? string.Empty);

            if (meta == null) return;
            var normalized = NormalizeMetadata(id, meta);
            try { WriteAtomically(MetadataPath(id), SerializeMetadata(normalized)); }
            catch { /* sidecar is reconstructible from payload; never fail the payload commit */ }
        }

        public bool SlotExists(int slot) => File.Exists(SlotPath(slot));

        public bool SlotExists(SaveSlotId id) => File.Exists(SlotPath(id));

        /// <summary>Read a slot's JSON. On a missing or unreadable file returns false. If the file
        /// exists but <paramref name="isValid"/> rejects it (corrupt content), the bad file is moved
        /// aside to slot_{n}.json.corrupt (or .corrupt.2, .corrupt.3, … to preserve earlier corrupt
        /// saves) so it can't crash the loader and the slot is freed.</summary>
        public bool TryLoad(int slot, Func<string, bool> isValid, out string json)
            => TryLoadCore(SlotPath(slot), isValid, out json);

        /// <summary>Read payload JSON for a named slot. Invalid payloads are quarantined.</summary>
        public bool TryLoadPayload(SaveSlotId id, Func<string, bool> isValid, out string payloadJson)
            => TryLoadCore(SlotPath(id), isValid, out payloadJson);

        /// <summary>Read sidecar metadata for a named slot. Missing/corrupt sidecar returns false.</summary>
        public bool TryLoadMetadata(SaveSlotId id, out SaveSlotMetadata meta)
        {
            meta = null;
            var path = MetadataPath(id);
            if (!File.Exists(path)) return false;

            string raw;
            try { raw = File.ReadAllText(path); }
            catch { return false; }

            if (!TryParseMetadata(raw, out var parsed)) return false;
            meta = NormalizeMetadata(id, parsed);
            return true;
        }

        /// <summary>Delete payload+sidecar+corrupt siblings for a named slot. Best-effort and
        /// non-throwing; returns whether the payload existed before delete.</summary>
        public bool Delete(SaveSlotId id)
        {
            var payloadPath = SlotPath(id);
            var sidecarPath = MetadataPath(id);
            var existed = File.Exists(payloadPath);

            TryDelete(payloadPath);
            TryDelete(sidecarPath);
            TryDeleteCorruptSiblings(payloadPath);
            return existed;
        }

        private static void Quarantine(string path)
        {
            try
            {
                // DET-06: preserve every corrupt save for forensics instead of overwriting the last
                // one. Pick the first free ".corrupt[.N]" name — deterministic, no wall-clock (keeps
                // this Data type free of DateTime per the determinism guard).
                var dest = path + ".corrupt";
                int n = 1;
                while (File.Exists(dest)) dest = path + ".corrupt." + (++n);
                File.Move(path, dest);
            }
            catch { /* best-effort; never throw from the load path */ }
        }

        /// <summary>The slot indices (0..maxSlots-1) that currently hold a save file.</summary>
        public IReadOnlyList<int> ListSlots(int maxSlots = 16)
        {
            var slots = new List<int>();
            for (int i = 0; i < maxSlots; i++)
                if (SlotExists(i)) slots.Add(i);
            return slots;
        }

        /// <summary>Deterministic slot list order: Quick, Auto, then Manual ascending.</summary>
        public IReadOnlyList<SaveSlotMetadata> ListAll(int manualCap)
        {
            if (manualCap < 0) throw new ArgumentOutOfRangeException(nameof(manualCap), "manualCap must be >= 0.");
            var all = new List<SaveSlotMetadata>();
            AddMetadataIfSlotExists(all, SaveSlotId.Quick);
            AddMetadataIfSlotExists(all, SaveSlotId.Auto);
            for (int i = 0; i < manualCap; i++)
                AddMetadataIfSlotExists(all, SaveSlotId.Manual(i));
            return all;
        }

        private void AddMetadataIfSlotExists(List<SaveSlotMetadata> all, SaveSlotId id)
        {
            if (!SlotExists(id)) return;
            if (TryLoadMetadata(id, out var loaded))
            {
                all.Add(loaded);
                return;
            }

            if (TryReconstructMetadata(id, out var reconstructed))
            {
                all.Add(reconstructed);
                return;
            }

            all.Add(CreateDefaultMetadata(id));
        }

        private bool TryLoadCore(string path, Func<string, bool> isValid, out string json)
        {
            json = null;
            if (!File.Exists(path)) return false;

            string raw;
            try { raw = File.ReadAllText(path); }
            catch { return false; }

            if (isValid != null && !isValid(raw))
            {
                Quarantine(path);
                return false;
            }

            json = raw;
            return true;
        }

        private static void WriteAtomically(string path, string content)
        {
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, content ?? string.Empty);
            // DET-07: atomic publish. File.Replace swaps tmp into place in a single operation (atomic
            // on NTFS), so a crash can't leave the slot deleted-but-not-yet-moved (the old
            // Delete-then-Move had exactly that window). Move only when there is no existing slot.
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch { /* best-effort */ }
        }

        private void TryDeleteCorruptSiblings(string payloadPath)
        {
            try
            {
                if (!Directory.Exists(_savesDir)) return;
                var prefix = Path.GetFileName(payloadPath) + ".corrupt";
                foreach (var file in Directory.GetFiles(_savesDir))
                {
                    if (!Path.GetFileName(file).StartsWith(prefix, StringComparison.Ordinal)) continue;
                    TryDelete(file);
                }
            }
            catch { /* best-effort */ }
        }

        private bool TryReconstructMetadata(SaveSlotId id, out SaveSlotMetadata meta)
        {
            meta = null;
            if (!TryLoadPayload(id, _ => true, out var payload)) return false;

            var reconstructed = CreateDefaultMetadata(id);
            if (TryExtractJsonStringField(payload, "sceneName", out var sceneName))
                reconstructed.sceneName = sceneName;

            if (TryExtractJsonIntField(payload, "envelopeVersion", out var envelopeVersion))
                reconstructed.envelopeVersion = envelopeVersion;

            if (TryExtractJsonStringField(payload, "domainStateJson", out var domainStateJson))
            {
                if (TryExtractJsonLongField(domainStateJson, "totalMinutes", out var totalMinutes))
                    reconstructed.playtimeMinutes = totalMinutes;
                if (TryExtractJsonIntField(domainStateJson, "schemaVersion", out var schemaVersion))
                    reconstructed.schemaVersion = schemaVersion;
            }

            meta = reconstructed;
            return true;
        }

        private static SaveSlotMetadata CreateDefaultMetadata(SaveSlotId id)
        {
            return new SaveSlotMetadata
            {
                metadataVersion = DefaultMetadataVersion,
                envelopeVersion = 0,
                schemaVersion = 0,
                slotKind = id.Kind.ToString(),
                slotIndex = id.Kind == SaveSlotKind.Manual ? id.Index : 0,
                label = DefaultLabel(id),
                sceneName = string.Empty,
                playtimeMinutes = 0,
                savedAtUtcIso = string.Empty,
                thumbnailPath = string.Empty
            };
        }

        private static SaveSlotMetadata NormalizeMetadata(SaveSlotId id, SaveSlotMetadata meta)
        {
            var normalized = new SaveSlotMetadata
            {
                metadataVersion = meta.metadataVersion > 0 ? meta.metadataVersion : DefaultMetadataVersion,
                envelopeVersion = meta.envelopeVersion,
                schemaVersion = meta.schemaVersion,
                slotKind = id.Kind.ToString(),
                slotIndex = id.Kind == SaveSlotKind.Manual ? id.Index : 0,
                label = string.IsNullOrWhiteSpace(meta.label) ? DefaultLabel(id) : meta.label,
                sceneName = meta.sceneName ?? string.Empty,
                playtimeMinutes = meta.playtimeMinutes,
                savedAtUtcIso = meta.savedAtUtcIso ?? string.Empty,
                thumbnailPath = meta.thumbnailPath ?? string.Empty
            };
            return normalized;
        }

        private static string DefaultLabel(SaveSlotId id)
        {
            switch (id.Kind)
            {
                case SaveSlotKind.Auto: return "Autosave";
                case SaveSlotKind.Quick: return "Quicksave";
                case SaveSlotKind.Manual: return "Slot " + (id.Index + 1);
                default: return "Slot";
            }
        }

        private static string SerializeMetadata(SaveSlotMetadata meta)
        {
            var sb = new StringBuilder(256);
            sb.Append('{');
            sb.Append("\"metadataVersion\":").Append(meta.metadataVersion.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"envelopeVersion\":").Append(meta.envelopeVersion.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"schemaVersion\":").Append(meta.schemaVersion.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"slotKind\":\"").Append(EscapeJson(meta.slotKind)).Append("\",");
            sb.Append("\"slotIndex\":").Append(meta.slotIndex.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"label\":\"").Append(EscapeJson(meta.label)).Append("\",");
            sb.Append("\"sceneName\":\"").Append(EscapeJson(meta.sceneName)).Append("\",");
            sb.Append("\"playtimeMinutes\":").Append(meta.playtimeMinutes.ToString(CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"savedAtUtcIso\":\"").Append(EscapeJson(meta.savedAtUtcIso)).Append("\",");
            sb.Append("\"thumbnailPath\":\"").Append(EscapeJson(meta.thumbnailPath)).Append("\"");
            sb.Append('}');
            return sb.ToString();
        }

        private static bool TryParseMetadata(string raw, out SaveSlotMetadata meta)
        {
            meta = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            if (!TryExtractJsonIntField(raw, "metadataVersion", out var metadataVersion)) return false;

            TryExtractJsonIntField(raw, "envelopeVersion", out var envelopeVersion);
            TryExtractJsonIntField(raw, "schemaVersion", out var schemaVersion);
            TryExtractJsonStringField(raw, "slotKind", out var slotKind);
            TryExtractJsonIntField(raw, "slotIndex", out var slotIndex);
            TryExtractJsonStringField(raw, "label", out var label);
            TryExtractJsonStringField(raw, "sceneName", out var sceneName);
            TryExtractJsonLongField(raw, "playtimeMinutes", out var playtimeMinutes);
            TryExtractJsonStringField(raw, "savedAtUtcIso", out var savedAtUtcIso);
            TryExtractJsonStringField(raw, "thumbnailPath", out var thumbnailPath);

            meta = new SaveSlotMetadata
            {
                metadataVersion = metadataVersion,
                envelopeVersion = envelopeVersion,
                schemaVersion = schemaVersion,
                slotKind = slotKind ?? string.Empty,
                slotIndex = slotIndex,
                label = label ?? string.Empty,
                sceneName = sceneName ?? string.Empty,
                playtimeMinutes = playtimeMinutes,
                savedAtUtcIso = savedAtUtcIso ?? string.Empty,
                thumbnailPath = thumbnailPath ?? string.Empty
            };
            return true;
        }

        private static bool TryExtractJsonStringField(string raw, string fieldName, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(raw)) return false;

            var pattern = string.Format(CultureInfo.InvariantCulture, JsonStringFieldPatternTemplate.ToString(), Regex.Escape(fieldName));
            var match = Regex.Match(raw, pattern, RegexOptions.CultureInvariant);
            if (!match.Success) return false;

            value = UnescapeJson(match.Groups[1].Value);
            return true;
        }

        private static bool TryExtractJsonIntField(string raw, string fieldName, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(raw)) return false;
            var pattern = string.Format(CultureInfo.InvariantCulture, JsonIntFieldPatternTemplate.ToString(), Regex.Escape(fieldName));
            var match = Regex.Match(raw, pattern, RegexOptions.CultureInvariant);
            if (!match.Success) return false;
            return int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryExtractJsonLongField(string raw, string fieldName, out long value)
        {
            value = 0L;
            if (string.IsNullOrEmpty(raw)) return false;
            var pattern = string.Format(CultureInfo.InvariantCulture, JsonIntFieldPatternTemplate.ToString(), Regex.Escape(fieldName));
            var match = Regex.Match(raw, pattern, RegexOptions.CultureInvariant);
            if (!match.Success) return false;
            return long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length + 8);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private static string UnescapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (c != '\\' || i == value.Length - 1)
                {
                    sb.Append(c);
                    continue;
                }

                i++;
                var escaped = value[i];
                switch (escaped)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 < value.Length)
                        {
                            var hex = value.Substring(i + 1, 4);
                            if (ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                            {
                                sb.Append((char)code);
                                i += 4;
                                break;
                            }
                        }
                        sb.Append('u');
                        break;
                    default:
                        sb.Append(escaped);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
