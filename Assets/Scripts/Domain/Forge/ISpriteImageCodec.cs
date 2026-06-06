namespace EmberCrpg.Domain.Forge
{
    public interface ISpriteImageCodec
    {
        SpriteImageFrame Decode(byte[] encodedBytes);
        byte[] Encode(SpriteImageFrame frame);
    }
}
