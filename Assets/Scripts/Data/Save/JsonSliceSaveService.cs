using EmberCrpg.Domain.World;
using UnityEngine;

// Design note:
// JsonSliceSaveService converts Sprint 1 world state to and from JSON text.
// Inputs: pure world snapshots or JSON strings.
// Outputs: pretty JSON and reconstructed world state via DTO mapping.
// Bible reference: PRD FR-06.
namespace EmberCrpg.Data.Save
{
    /// <summary>JsonUtility-backed save/load bridge for the vertical slice.</summary>
    public sealed class JsonSliceSaveService
    {
        public string SaveToJson(SliceWorldState world)
        {
            return JsonUtility.ToJson(SliceSaveMapper.ToData(world), true);
        }

        public SliceWorldState LoadFromJson(string json)
        {
            return SliceSaveMapper.ToWorld(JsonUtility.FromJson<SliceSaveData>(json));
        }
    }
}
