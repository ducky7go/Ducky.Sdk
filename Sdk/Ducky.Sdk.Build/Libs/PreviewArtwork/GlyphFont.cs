using System;
using System.Collections.Generic;

/// <summary>
/// Draws an abbreviation with an embedded 5x7 bitmap font (A-Z and 0-9 only), centred on an RGB buffer.
/// Text is white with a one-cell dark shadow. Other characters are drawn as blank space.
/// </summary>
internal static class GlyphFont
{
    public const int GlyphWidth = 5;
    public const int GlyphHeight = 7;
    private const int Advance = GlyphWidth + 1; // one blank column between letters

    /// <summary>The text rectangle, including the shadow, in pixels.</summary>
    public readonly record struct TextRect(int X, int Y, int Width, int Height);

    // '#' is a lit cell, '.' is an unlit cell. Rows are top to bottom.
    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['A'] = [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
        ['B'] = ["####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."],
        ['C'] = [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."],
        ['D'] = ["###..", "#..#.", "#...#", "#...#", "#...#", "#..#.", "###.."],
        ['E'] = ["#####", "#....", "#....", "####.", "#....", "#....", "#####"],
        ['F'] = ["#####", "#....", "#....", "####.", "#....", "#....", "#...."],
        ['G'] = [".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####"],
        ['H'] = ["#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"],
        ['I'] = [".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###."],
        ['J'] = ["..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.."],
        ['K'] = ["#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#"],
        ['L'] = ["#....", "#....", "#....", "#....", "#....", "#....", "#####"],
        ['M'] = ["#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#"],
        ['N'] = ["#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#"],
        ['O'] = [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
        ['P'] = ["####.", "#...#", "#...#", "####.", "#....", "#....", "#...."],
        ['Q'] = [".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#"],
        ['R'] = ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"],
        ['S'] = [".####", "#....", "#....", ".###.", "....#", "....#", "####."],
        ['T'] = ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."],
        ['U'] = ["#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."],
        ['V'] = ["#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.."],
        ['W'] = ["#...#", "#...#", "#...#", "#.#.#", "#.#.#", "#.#.#", ".#.#."],
        ['X'] = ["#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"],
        ['Y'] = ["#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.."],
        ['Z'] = ["#####", "....#", "...#.", "..#..", ".#...", "#....", "#####"],
        ['0'] = [".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###."],
        ['1'] = ["..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###."],
        ['2'] = [".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"],
        ['3'] = ["#####", "...#.", "..#..", "...#.", "....#", "#...#", ".###."],
        ['4'] = ["...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."],
        ['5'] = ["#####", "#....", "####.", "....#", "....#", "#...#", ".###."],
        ['6'] = ["..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###."],
        ['7'] = ["#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."],
        ['8'] = [".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."],
        ['9'] = [".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.."],
    };

    /// <summary>
    /// Largest integer scale such that the text is at most 70% of the image width and 70% of its height.
    /// The height bound keeps a one-letter abbreviation inside the image.
    /// </summary>
    public static int ScaleFor(int width, int height, int letterCount)
    {
        var cells = Advance * letterCount - 1;
        var byWidth = (width * 7 / 10) / cells;
        var byHeight = (height * 7 / 10) / GlyphHeight;
        return Math.Max(1, Math.Min(byWidth, byHeight));
    }

    /// <summary>The rectangle that <see cref="DrawCentered"/> would fill, without drawing anything.</summary>
    public static TextRect MeasureCentered(int width, int height, string text)
    {
        var letters = text.Length;
        if (letters == 0) return new TextRect(0, 0, 0, 0);

        var scale = ScaleFor(width, height, letters);
        var blockWidth = (Advance * letters - 1) * scale;
        var blockHeight = GlyphHeight * scale;
        var left = (width - blockWidth) / 2;
        var top = (height - blockHeight) / 2;

        // The shadow adds one cell to the right and below.
        return new TextRect(left, top, blockWidth + scale, blockHeight + scale);
    }

    /// <summary>
    /// Draws <paramref name="text"/> centred on a tightly packed 8-bit RGB buffer: a dark shadow first, then white cells on top.
    /// Pixels outside the returned rectangle are not touched.
    /// </summary>
    public static TextRect DrawCentered(byte[] rgb, int width, int height, string text)
    {
        var rect = MeasureCentered(width, height, text);
        if (rect.Width == 0) return rect;

        var scale = ScaleFor(width, height, text.Length);
        var left = (width - (Advance * text.Length - 1) * scale) / 2;
        var top = (height - GlyphHeight * scale) / 2;

        // Pass 1: shadow (one cell offset, black). Pass 2: white glyph cells on top.
        for (var pass = 0; pass < 2; pass++)
        {
            var offset = pass == 0 ? scale : 0;
            var value = pass == 0 ? (byte)0 : (byte)255;

            for (var letter = 0; letter < text.Length; letter++)
            {
                var glyph = GlyphFor(text[letter]);
                if (glyph == null) continue;

                var originX = left + letter * Advance * scale + offset;
                var originY = top + offset;
                for (var row = 0; row < GlyphHeight; row++)
                for (var col = 0; col < GlyphWidth; col++)
                {
                    if (glyph[row][col] != '#') continue;
                    FillCell(rgb, width, originX + col * scale, originY + row * scale, scale, value);
                }
            }
        }

        return rect;
    }

    private static string[]? GlyphFor(char c)
    {
        var upper = char.ToUpperInvariant(c);
        return Glyphs.TryGetValue(upper, out var glyph) ? glyph : null;
    }

    private static void FillCell(byte[] rgb, int width, int x0, int y0, int size, byte value)
    {
        for (var y = y0; y < y0 + size; y++)
        for (var x = x0; x < x0 + size; x++)
        {
            var i = (y * width + x) * 3;
            rgb[i] = value;
            rgb[i + 1] = value;
            rgb[i + 2] = value;
        }
    }
}
