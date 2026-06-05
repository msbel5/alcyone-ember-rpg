using System;

namespace EmberCrpg.Data.GeneratedAssets
{
    [Serializable]
    public struct PixelRect
    {
        public int x;
        public int y;
        public int width;
        public int height;

        public PixelRect(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }
}
