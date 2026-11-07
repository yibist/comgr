using System;
using System.IO;
using System.Numerics;
using Avalonia;
using Avalonia.Media.Imaging;

public static class TextureLoader
{
    public static Texture LoadFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        using var bitmap = new Bitmap(stream);
        return FromBitmap(bitmap);
    }

    public static Texture FromBitmap(Bitmap bitmap)
    {
        int width = bitmap.PixelSize.Width;
        int height = bitmap.PixelSize.Height;
        var pixels = new Vector3[width * height];

        int stride = width * 4;
        var buffer = new byte[stride * height];

        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                bitmap.CopyPixels(
                    new PixelRect(0, 0, width, height),
                    (IntPtr)ptr,
                    buffer.Length,
                    stride);
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                byte b = buffer[i + 0];
                byte g = buffer[i + 1];
                byte r = buffer[i + 2];

                float lr = SrgbToLinear(r / 255f);
                float lg = SrgbToLinear(g / 255f);
                float lb = SrgbToLinear(b / 255f);

                pixels[y * width + x] = new Vector3(lr, lg, lb);
            }
        }

        return new Texture(width, height, pixels);
    }

    public static float SrgbToLinear(float c)
    {
        if (c <= 0.04045f) return c / 12.92f;
        return MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    public static float LinearToSrgb(float c)
    {
        if (c <= 0.0031308f) return c * 12.92f;
        return 1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f;
    }
}

public class Texture
{
    public readonly int Width;
    public readonly int Height;
    public readonly Vector3[] Pixels;

    public Texture(int width, int height, Vector3[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public Vector3 Sample(float u, float v)
    {
        u -= MathF.Floor(u);
        v -= MathF.Floor(v);
        
        float x = u * Width  - 0.5f;
        float y = v * Height - 0.5f;

        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);
        float fx = x - x0;
        float fy = y - y0;

        int xa = ((x0 % Width)  + Width) % Width;
        int xb = (xa + 1) % Width;
        int ya = ((y0 % Height) + Height) % Height;
        int yb = (ya + 1) % Height;

        Vector3 c00 = Pixels[ya*Height + xa];
        Vector3 c10 = Pixels[ya*Height + xb];
        Vector3 c01 =  Pixels[yb*Height + xa];
        Vector3 c11 =  Pixels[yb*Height + xb];

        Vector3 top    = Vector3.Lerp(c00, c10, fx);
        Vector3 bottom = Vector3.Lerp(c01, c11, fx);
        return Vector3.Lerp(top, bottom, fy);
    }
}