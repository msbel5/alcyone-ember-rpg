using System;
using System.Collections.Generic;
using System.IO;

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
        private readonly string _savesDir;

        public FileSaveRepository(string rootDir)
        {
            if (string.IsNullOrWhiteSpace(rootDir)) throw new ArgumentException("rootDir required", nameof(rootDir));
            _savesDir = Path.Combine(rootDir, "saves");
        }

        public string SlotPath(int slot) => Path.Combine(_savesDir, "slot_" + slot + ".json");

        /// <summary>Write the save JSON for a slot (creates the saves dir). Atomic-ish: writes a
        /// .tmp then moves into place so a crash mid-write can't corrupt an existing slot.</summary>
        public void Save(int slot, string json)
        {
            Directory.CreateDirectory(_savesDir);
            var path = SlotPath(slot);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json ?? string.Empty);
            // DET-07: atomic publish. File.Replace swaps tmp into place in a single operation (atomic
            // on NTFS), so a crash can't leave the slot deleted-but-not-yet-moved (the old
            // Delete-then-Move had exactly that window). Move only when there is no existing slot.
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        public bool SlotExists(int slot) => File.Exists(SlotPath(slot));

        /// <summary>Read a slot's JSON. On a missing or unreadable file returns false. If the file
        /// exists but <paramref name="isValid"/> rejects it (corrupt content), the bad file is moved
        /// aside to slot_{n}.json.corrupt (or .corrupt.2, .corrupt.3, … to preserve earlier corrupt
        /// saves) so it can't crash the loader and the slot is freed.</summary>
        public bool TryLoad(int slot, Func<string, bool> isValid, out string json)
        {
            json = null;
            var path = SlotPath(slot);
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
    }
}
