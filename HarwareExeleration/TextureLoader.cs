using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public static class TextureLoader
{
    public static Texture LoadFromFile(string path)
    {
        using var image = Image.Load<Rgba32>(path);
        return FromImage(image);
    }

    public static Texture FromImage(Image<Rgba32> image)
    {
        int width = image.Width;
        int height = image.Height;
        var pixels = new byte[width * height * 3];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                Span<Rgba32> row = accessor.GetRowSpan(y);
                int dstOffset = y * width * 3;

                for (int x = 0; x < width; x++)
                {
                    Rgba32 p = row[x];
                    int j = dstOffset + x * 3;
                    // ImageSharp Rgba32 is already R, G, B, A order
                    pixels[j + 0] = p.R;
                    pixels[j + 1] = p.G;
                    pixels[j + 2] = p.B;
                }
            }
        });

        return new Texture(width, height, pixels);
    }
}

public class Texture
{
    public readonly int Width;
    public readonly int Height;

    // Tightly packed sRGB, 3 bytes per pixel: [R,G,B, R,G,B, ...]
    public readonly byte[] Pixels;

    public Texture(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }
}