namespace EmberCrpg.Domain.Forge
{
    public interface IImageMatteService
    {
        MatteResult Matte(System.ReadOnlySpan<byte> rgba, int width, int height);
    }
}
