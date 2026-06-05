using System;
using System.Collections.Generic;
using EmberCrpg.Data.Save;
using EmberCrpg.Presentation.Ember.UI;

namespace EmberCrpg.Presentation.Ember.Save
{
    public sealed partial class EmberSaveService
    {
        public int ManualSlotCap => _repo != null ? _repo.ManualCapDefault : 10;

        public IReadOnlyList<SaveSlotMetadata> ListSlots()
        {
            return _repo != null ? _repo.ListAll(ManualSlotCap) : Array.Empty<SaveSlotMetadata>();
        }

        public SaveLoadActionResult SaveFromUi(SaveSlotId slot)
        {
            try
            {
                SaveInternal(slot);
                return new SaveLoadActionResult(true, "Saved " + SaveSlotLabelFormatter.Title(slot) + ".");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[EmberSave] ui-save failed: " + ex);
                ShowStatus("Save failed.");
                return new SaveLoadActionResult(false, "Save failed.");
            }
        }

        public SaveLoadActionResult LoadFromUi(SaveSlotId slot)
        {
            try
            {
                if (_repo == null || !_repo.TryLoadPayload(slot, AuditIsLoadableSaveJson, out var json))
                {
                    ShowStatus("No save found.");
                    return new SaveLoadActionResult(false, "No save found.");
                }

                LoadJson(json, slot);
                return new SaveLoadActionResult(true, "Loaded " + SaveSlotLabelFormatter.Title(slot) + ".");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[EmberSave] ui-load failed: " + ex);
                ShowStatus("Load failed.");
                return new SaveLoadActionResult(false, "Load failed.");
            }
        }

        public SaveLoadActionResult LoadLatestFromUi()
        {
            try
            {
                if (!TryResolveLatestSave(out var data))
                {
                    ShowStatus("No save found.");
                    return new SaveLoadActionResult(false, "No save found.");
                }

                LoadJson(SaveEnvelopeCodec.Encode(data), null);
                return new SaveLoadActionResult(true, "Loaded last save.");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[EmberSave] ui-load-latest failed: " + ex);
                ShowStatus("Load failed.");
                return new SaveLoadActionResult(false, "Load failed.");
            }
        }
    }
}
