using System;

// REF-f (LEFT/66-LOC): WorldProcess DTOs split out of WorldSaveData.cs (same namespace, zero behaviour change).
namespace EmberCrpg.Data.Save
{
    [Serializable]
    public sealed class ItemRecordSaveData
    {
        public long id;
        public int material;
        public int quality;
        public int slot;
        public string slotCode;
    }

    [Serializable]
    public sealed class SiteRecordSaveData
    {
        public long id;
        public int kind;
        public string name;
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
    }

    [Serializable]
    public sealed class WorksiteSaveData
    {
        public long siteId;
        public int positionX;
        public int positionY;
        public int kind;
        public bool isActive;
    }

    [Serializable]
    public sealed class RecipeWorkOrderSaveData
    {
        public long recipeId;
        public long siteId;
        public int positionX;
        public int positionY;
        public long actorId;
        public int progressTicks;
    }

    [Serializable]
    public sealed class SoilComponentSaveData
    {
        public long id;
        public long siteId;
        public int positionX;
        public int positionY;
        public int fertility;
        public int moisture;
        public long plantId;
    }

    [Serializable]
    public sealed class PlantComponentSaveData
    {
        public long id;
        public long siteId;
        public int positionX;
        public int positionY;
        public string speciesId;
        public string stageId;
        public int daysInStage;
    }
}
