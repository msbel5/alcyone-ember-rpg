namespace EmberCrpg.Domain.Forge
{
    public interface IResourceProbe
    {
        long AvailableVideoMemoryMb();
        long AvailableSystemMemoryMb();
    }

    public sealed class NullResourceProbe : IResourceProbe
    {
        public long AvailableVideoMemoryMb() => long.MaxValue;
        public long AvailableSystemMemoryMb() => long.MaxValue;
    }
}
