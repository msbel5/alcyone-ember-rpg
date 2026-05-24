using System;
using System.IO;

namespace EmberCrpg.Simulation.Forge
{
    internal static class OnnxPngEncoder
    {
        public static byte[] EncodeGrayscale(int width, int height, byte[] pixels)
        {
            return Encode(width, height, pixels, channels: 1);
        }

        public static byte[] EncodeRgb(int width, int height, byte[] pixels)
        {
            return Encode(width, height, pixels, channels: 3);
        }

        public static byte[] EncodeRgba(int width, int height, byte[] pixels)
        {
            return Encode(width, height, pixels, channels: 4);
        }

        public static byte[] Encode(int width, int height, byte[] pixels, int channels)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (channels != 1 && channels != 3 && channels != 4)
                throw new ArgumentOutOfRangeException(nameof(channels), "PNG encoder supports 1, 3, or 4 channels.");

            int rowBytes = width * channels;
            int expected = rowBytes * height;
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (pixels.Length != expected)
                throw new ArgumentException("Pixel buffer length mismatch.", nameof(pixels));

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
                WriteChunk(bw, "IHDR", BuildIhdr(width, height, channels));

                var raw = new byte[(rowBytes + 1) * height];
                int src = 0;
                int dst = 0;
                for (int y = 0; y < height; y++)
                {
                    raw[dst++] = 0;
                    Buffer.BlockCopy(pixels, src, raw, dst, rowBytes);
                    src += rowBytes;
                    dst += rowBytes;
                }

                WriteChunk(bw, "IDAT", ZlibStore(raw));
                WriteChunk(bw, "IEND", new byte[0]);
                bw.Flush();
                return ms.ToArray();
            }
        }

        private static byte[] BuildIhdr(int width, int height, int channels)
        {
            var ihdr = new byte[13];
            WriteBigEndianInt(ihdr, 0, width);
            WriteBigEndianInt(ihdr, 4, height);
            ihdr[8] = 8;
            ihdr[9] = channels == 1 ? (byte)0 : channels == 3 ? (byte)2 : (byte)6;
            ihdr[10] = 0;
            ihdr[11] = 0;
            ihdr[12] = 0;
            return ihdr;
        }

        private static void WriteChunk(BinaryWriter bw, string type, byte[] data)
        {
            var typeBytes = new[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
            var lenBytes = new byte[4];
            WriteBigEndianInt(lenBytes, 0, data.Length);
            bw.Write(lenBytes);
            bw.Write(typeBytes);
            if (data.Length > 0) bw.Write(data);

            var crcBuf = new byte[4 + data.Length];
            Buffer.BlockCopy(typeBytes, 0, crcBuf, 0, 4);
            if (data.Length > 0) Buffer.BlockCopy(data, 0, crcBuf, 4, data.Length);
            var crcBytes = new byte[4];
            WriteBigEndianInt(crcBytes, 0, (int)Crc32(crcBuf));
            bw.Write(crcBytes);
        }

        private static byte[] ZlibStore(byte[] raw)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write((byte)0x78);
                bw.Write((byte)0x01);

                int pos = 0;
                while (pos < raw.Length)
                {
                    int chunk = Math.Min(65535, raw.Length - pos);
                    bool last = (pos + chunk) == raw.Length;
                    bw.Write((byte)(last ? 1 : 0));
                    bw.Write((byte)(chunk & 0xFF));
                    bw.Write((byte)((chunk >> 8) & 0xFF));
                    int nchunk = ~chunk;
                    bw.Write((byte)(nchunk & 0xFF));
                    bw.Write((byte)((nchunk >> 8) & 0xFF));
                    bw.Write(raw, pos, chunk);
                    pos += chunk;
                }

                var adlerBytes = new byte[4];
                WriteBigEndianInt(adlerBytes, 0, (int)Adler32(raw));
                bw.Write(adlerBytes);
                return ms.ToArray();
            }
        }

        private static void WriteBigEndianInt(byte[] dst, int offset, int value)
        {
            dst[offset + 0] = (byte)((value >> 24) & 0xFF);
            dst[offset + 1] = (byte)((value >> 16) & 0xFF);
            dst[offset + 2] = (byte)((value >> 8) & 0xFF);
            dst[offset + 3] = (byte)(value & 0xFF);
        }

        private static uint Adler32(byte[] data)
        {
            const uint mod = 65521;
            uint a = 1;
            uint b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % mod;
                b = (b + a) % mod;
            }
            return (b << 16) | a;
        }

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? (0xEDB88320u ^ (c >> 1)) : (c >> 1);
                table[n] = c;
            }
            return table;
        }

        private static uint Crc32(byte[] data)
        {
            uint c = 0xFFFFFFFFu;
            for (int i = 0; i < data.Length; i++)
                c = CrcTable[(c ^ data[i]) & 0xFF] ^ (c >> 8);
            return c ^ 0xFFFFFFFFu;
        }
    }
}
