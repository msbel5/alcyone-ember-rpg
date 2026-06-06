using System;
using System.Collections.Generic;
using System.IO;
using EmberCrpg.Domain.Forge;

namespace EmberCrpg.Simulation.Forge
{
    internal static class OnnxPngDecoder
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

        public static SpriteImageFrame Decode(byte[] pngBytes)
        {
            if (pngBytes == null) throw new ArgumentNullException(nameof(pngBytes));
            using var stream = new MemoryStream(pngBytes, writable: false);
            using var reader = new BinaryReader(stream);
            VerifySignature(reader);

            var width = 0;
            var height = 0;
            var colorType = -1;
            var idat = new List<byte>(pngBytes.Length);

            while (stream.Position < stream.Length)
            {
                var length = ReadInt32BigEndian(reader);
                var type = new string(reader.ReadChars(4));
                var payload = reader.ReadBytes(length);
                _ = reader.ReadUInt32();

                if (type == "IHDR")
                {
                    width = ReadInt32BigEndian(payload, 0);
                    height = ReadInt32BigEndian(payload, 4);
                    var bitDepth = payload[8];
                    colorType = payload[9];
                    if (bitDepth != 8) throw new InvalidOperationException("Only 8-bit PNGs are supported.");
                    if (colorType != 0 && colorType != 2 && colorType != 6)
                        throw new InvalidOperationException("Unsupported PNG color type " + colorType + ".");
                }
                else if (type == "IDAT")
                {
                    idat.AddRange(payload);
                }
                else if (type == "IEND")
                {
                    break;
                }
            }

            if (width <= 0 || height <= 0) throw new InvalidOperationException("PNG missing IHDR.");
            if (idat.Count == 0) throw new InvalidOperationException("PNG missing IDAT.");

            var channels = colorType == 6 ? 4 : colorType == 2 ? 3 : 1;
            var scanlines = InflateStoreBlocks(idat.ToArray(), height * ((width * channels) + 1));
            return new SpriteImageFrame(width, height, ExpandToRgba(scanlines, width, height, channels));
        }

        private static void VerifySignature(BinaryReader reader)
        {
            var actual = reader.ReadBytes(Signature.Length);
            if (actual.Length != Signature.Length) throw new InvalidOperationException("Invalid PNG signature.");
            for (var i = 0; i < Signature.Length; i++)
                if (actual[i] != Signature[i])
                    throw new InvalidOperationException("Invalid PNG signature.");
        }

        private static byte[] InflateStoreBlocks(byte[] zlibBytes, int expectedRawBytes)
        {
            if (zlibBytes.Length < 6) throw new InvalidOperationException("Invalid zlib payload.");
            var position = 2; // zlib header
            var raw = new byte[expectedRawBytes];
            var written = 0;

            while (position < zlibBytes.Length - 4 && written < expectedRawBytes)
            {
                var blockHeader = zlibBytes[position++];
                var final = (blockHeader & 0x01) != 0;
                var blockType = (blockHeader >> 1) & 0x03;
                if (blockType != 0) throw new InvalidOperationException("Only store-block zlib payloads are supported.");

                var length = zlibBytes[position] | (zlibBytes[position + 1] << 8);
                var inverse = zlibBytes[position + 2] | (zlibBytes[position + 3] << 8);
                position += 4;
                if ((length ^ 0xFFFF) != inverse) throw new InvalidOperationException("Corrupt zlib store block.");

                Buffer.BlockCopy(zlibBytes, position, raw, written, length);
                position += length;
                written += length;
                if (final) break;
            }

            if (written != expectedRawBytes)
                throw new InvalidOperationException("Decoded PNG payload size mismatch.");
            return raw;
        }

        private static byte[] ExpandToRgba(byte[] scanlines, int width, int height, int channels)
        {
            var rgba = new byte[width * height * 4];
            var rowBytes = width * channels;
            var scanOffset = 0;
            var outOffset = 0;
            for (var y = 0; y < height; y++)
            {
                var filter = scanlines[scanOffset++];
                if (filter != 0) throw new InvalidOperationException("Unsupported PNG row filter " + filter + ".");

                for (var x = 0; x < width; x++)
                {
                    if (channels == 4)
                    {
                        rgba[outOffset + 0] = scanlines[scanOffset++];
                        rgba[outOffset + 1] = scanlines[scanOffset++];
                        rgba[outOffset + 2] = scanlines[scanOffset++];
                        rgba[outOffset + 3] = scanlines[scanOffset++];
                    }
                    else if (channels == 3)
                    {
                        rgba[outOffset + 0] = scanlines[scanOffset++];
                        rgba[outOffset + 1] = scanlines[scanOffset++];
                        rgba[outOffset + 2] = scanlines[scanOffset++];
                        rgba[outOffset + 3] = 255;
                    }
                    else
                    {
                        var gray = scanlines[scanOffset++];
                        rgba[outOffset + 0] = gray;
                        rgba[outOffset + 1] = gray;
                        rgba[outOffset + 2] = gray;
                        rgba[outOffset + 3] = 255;
                    }

                    outOffset += 4;
                }

                var expectedRowEnd = (y + 1) * (rowBytes + 1);
                if (scanOffset != expectedRowEnd)
                    throw new InvalidOperationException("Decoded PNG row size mismatch.");
            }

            return rgba;
        }

        private static int ReadInt32BigEndian(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            return ReadInt32BigEndian(bytes, 0);
        }

        private static int ReadInt32BigEndian(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24)
                | (bytes[offset + 1] << 16)
                | (bytes[offset + 2] << 8)
                | bytes[offset + 3];
        }
    }
}
