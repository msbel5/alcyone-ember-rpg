namespace EmberCrpg.Domain.Forge
{
    public sealed class SingleFigureGateResult
    {
        public SingleFigureGateResult(bool isSingleFigure, PixelBounds bounds, int componentCount, int upperBodyComponentCount, bool touchesFrameEdge, int opaquePixelCount, int mainComponentPixels, byte[] mainComponentMask)
        {
            IsSingleFigure = isSingleFigure;
            Bounds = bounds;
            ComponentCount = componentCount;
            UpperBodyComponentCount = upperBodyComponentCount;
            TouchesFrameEdge = touchesFrameEdge;
            OpaquePixelCount = opaquePixelCount;
            MainComponentPixels = mainComponentPixels;
            MainComponentMask = mainComponentMask;
        }

        public bool IsSingleFigure { get; }
        public PixelBounds Bounds { get; }
        public int ComponentCount { get; }
        public int UpperBodyComponentCount { get; }
        public bool TouchesFrameEdge { get; }
        public int OpaquePixelCount { get; }
        public int MainComponentPixels { get; }
        public byte[] MainComponentMask { get; }
    }
}
