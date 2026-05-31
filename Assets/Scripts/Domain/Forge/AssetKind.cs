namespace EmberCrpg.Domain.Forge
{
    public enum AssetKind
    {
        NpcBillboard = 0,
        Portrait = 1,
        Item = 2,
        Furniture = 3,
        Logo = 4,
        InventoryIcon = 5,
        EnvironmentProp = 6,
    }

    public static class AssetKindExtensions
    {
        public static AssetSubjectKind ToSubjectKind(this AssetKind kind)
        {
            switch (kind)
            {
                case AssetKind.NpcBillboard:
                case AssetKind.Portrait:
                    return AssetSubjectKind.Npc;

                case AssetKind.Item:
                case AssetKind.InventoryIcon:
                case AssetKind.Furniture:
                case AssetKind.Logo:
                    return AssetSubjectKind.Item;

                case AssetKind.EnvironmentProp:
                    return AssetSubjectKind.Region;

                default:
                    return AssetSubjectKind.Item;
            }
        }
    }
}
