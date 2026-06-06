using System;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Simulation.Forge
{
    public sealed class OnnxPngSpriteImageCodec : ISpriteImageCodec
    {
        public SpriteImageFrame Decode(byte[] encodedBytes)
        {
            if (encodedBytes == null) throw new ArgumentNullException(nameof(encodedBytes));
            return OnnxPngDecoder.Decode(encodedBytes);
        }

        public byte[] Encode(SpriteImageFrame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            return OnnxPngEncoder.EncodeRgba(frame.Width, frame.Height, frame.Rgba);
        }
    }
}
