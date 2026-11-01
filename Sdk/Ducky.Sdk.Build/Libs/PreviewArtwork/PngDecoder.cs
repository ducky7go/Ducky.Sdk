using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

/// <summary>A decoded image as tightly packed 8-bit RGB rows (alpha, if any, is dropped).</summary>
internal readonly record struct RgbImage(int Width, int Height, byte[] Pixels);

/// <summary>
/// Minimal PNG decoder for the preview backgrounds. It supports only what the pipeline produces:
/// non-interlaced, 8-bit RGB (colour type 2) or RGBA (colour type 6), filter types 0-4.
/// Ancillary chunks such as tEXt are ignored. Anything else throws, so a bad asset fails the build.
/// </summary>
internal static class PngDecoder
{
    private const int MaxDimension = 16384;
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static RgbImage DecodeRgb(byte[] png)
    {
        using var stream = new MemoryStream(png, writable: false);
        return DecodeRgb(stream);
    }

    public static RgbImage DecodeRgb(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var png = buffer.ToArray();

        if (png.Length < Signature.Length || !png.AsSpan(0, Signature.Length).SequenceEqual(Signature))
        {
            throw new InvalidDataException("Not a PNG file: the signature is missing.");
        }

        int width = 0, height = 0, bitsPerChannel = 0, colourType = -1;
        var idat = new MemoryStream();
        var offset = Signature.Length;
        var sawEnd = false;

        while (!sawEnd)
        {
            if (offset + 12 > png.Length) throw new InvalidDataException("Truncated PNG: a chunk header is incomplete.");

            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset, 4));
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            if (length < 0 || offset + 12L + length > png.Length)
            {
                throw new InvalidDataException($"Truncated PNG: chunk {type} is incomplete.");
            }

            var data = png.AsSpan(offset + 8, length);
            switch (type)
            {
                case "IHDR":
                    if (length != 13) throw new InvalidDataException("Invalid PNG: IHDR has the wrong length.");
                    width = BinaryPrimitives.ReadInt32BigEndian(data);
                    height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
                    bitsPerChannel = data[8];
                    colourType = data[9];
                    var compression = data[10];
                    var filterMethod = data[11];
                    var interlace = data[12];

                    if (bitsPerChannel != 8)
                        throw new NotSupportedException($"Unsupported PNG bit depth {bitsPerChannel}: only 8-bit images are supported.");
                    if (colourType != 2 && colourType != 6)
                        throw new NotSupportedException($"Unsupported PNG colour type {colourType}: only RGB (2) and RGBA (6) are supported.");
                    if (interlace != 0)
                        throw new NotSupportedException("Unsupported PNG: interlaced images are not supported.");
                    if (compression != 0 || filterMethod != 0)
                        throw new InvalidDataException("Invalid PNG: unknown compression or filter method.");
                    if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension)
                        throw new InvalidDataException($"Unsupported PNG size {width}x{height}.");
                    break;

                case "IDAT":
                    idat.Write(data);
                    break;

                case "IEND":
                    sawEnd = true;
                    break;
            }

            offset += 12 + length;
        }

        if (colourType < 0) throw new InvalidDataException("Invalid PNG: IHDR chunk is missing.");
        if (idat.Length == 0) throw new InvalidDataException("Invalid PNG: no IDAT data.");

        var bytesPerPixel = colourType == 6 ? 4 : 3;
        var stride = width * bytesPerPixel;
        var raw = Inflate(idat, (stride + 1) * height);
        var rgb = new byte[width * height * 3];

        var previous = new byte[stride];
        var current = new byte[stride];
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * (stride + 1);
            var filter = raw[rowStart];
            Array.Copy(raw, rowStart + 1, current, 0, stride);
            Unfilter(filter, current, previous, bytesPerPixel);

            for (var x = 0; x < width; x++)
            {
                Array.Copy(current, x * bytesPerPixel, rgb, (y * width + x) * 3, 3);
            }

            (previous, current) = (current, previous);
        }

        return new RgbImage(width, height, rgb);
    }

    private static byte[] Inflate(MemoryStream compressed, int expectedLength)
    {
        compressed.Position = 0;
        using var z = new ZLibStream(compressed, CompressionMode.Decompress);
        using var raw = new MemoryStream(expectedLength);
        z.CopyTo(raw);

        if (raw.Length != expectedLength)
        {
            throw new InvalidDataException($"Invalid PNG: image data is {raw.Length} bytes, expected {expectedLength}.");
        }

        return raw.ToArray();
    }

    /// <summary>Reverses one PNG scanline filter in place. <paramref name="previous"/> is the prior unfiltered row (zeros for the first).</summary>
    private static void Unfilter(byte filter, byte[] line, byte[] previous, int bpp)
    {
        for (var i = 0; i < line.Length; i++)
        {
            var a = i >= bpp ? line[i - bpp] : (byte)0; // left
            var b = previous[i];                        // up
            var c = i >= bpp ? previous[i - bpp] : (byte)0; // up-left

            switch (filter)
            {
                case 0: break;
                case 1: line[i] = (byte)(line[i] + a); break;
                case 2: line[i] = (byte)(line[i] + b); break;
                case 3: line[i] = (byte)(line[i] + ((a + b) >> 1)); break;
                case 4: line[i] = (byte)(line[i] + Paeth(a, b, c)); break;
                default: throw new InvalidDataException($"Invalid PNG: unknown filter type {filter}.");
            }
        }
    }

    private static byte Paeth(byte a, byte b, byte c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        return pb <= pc ? b : c;
    }
}
