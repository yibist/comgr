using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using Avalonia.Controls.ApplicationLifetimes;

namespace NoXmlImagePainter;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}

public class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}

public class MainWindow : Window
{
    private const int imageWidth = 800;
    private const int imageHeight = 800;
    private readonly WriteableBitmap _bitmap;

    public MainWindow()
    {
        Width = imageWidth;
        Height = imageHeight;
        Title = "Avalonia Bitmap Painter (No XML)";

        _bitmap = new WriteableBitmap(
            new PixelSize(imageWidth, imageHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        PaintImage();

        Content = new Image
        {
            Source = _bitmap,
            Stretch = Avalonia.Media.Stretch.None
        };
    }

    private unsafe void PaintImage()
    {
        using (ILockedFramebuffer fb = _bitmap.Lock())
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    
                    
                    // float a = 45f * MathF.PI / 180f;
                    // float cos = MathF.Cos(a), sin = MathF.Sin(a);
                    // float s = 2f;  // scale factor
                    //
                    // float rx = (x * cos - y * sin) * 2;
                    // float ry = (x * sin + y * cos) * 2;
                    double hue = ((double)(x + y) / (Width + Height)) * 360.0;
                    double c = 1.0;                              // chroma
                    double hp = hue / 60.0;
                    double xComp = c * (1 - Math.Abs(hp % 2 - 1));
                    double r1 = 0, g1 = 0, b1 = 0;

                    if (hp < 1)      { r1 = c; g1 = xComp; b1 = 0; }
                    else if (hp < 2) { r1 = xComp; g1 = c; b1 = 0; }
                    else if (hp < 3) { r1 = 0; g1 = c; b1 = xComp; }
                    else if (hp < 4) { r1 = 0; g1 = xComp; b1 = c; }
                    else if (hp < 5) { r1 = xComp; g1 = 0; b1 = c; }
                    else             { r1 = c; g1 = 0; b1 = xComp; }

                    byte red   = (byte)(r1 * 255);
                    byte green = (byte)(g1 * 255);
                    byte blue  = (byte)(b1 * 255);
                    float xi = x % 100;
                    float yi = y % 100;
                    



                    float TOLERANCE = 1f;
                    if (Math.Abs(xi + yi - 100) < TOLERANCE || Math.Abs(xi) < TOLERANCE || Math.Abs(yi) < TOLERANCE)
                    {
                        blue = 0;
                        red = 0;
                        green = 0;
                    }

                    

                    float  r = x / 25f;
                    
                    
                    
                    float diviser = 100 / 2f ;
                    
                    
                    // byte blue = 0;
                    // byte red = (byte) Math.Clamp(Math.Sin(Math.Sqrt(xi * xi + yi * yi) / diviser * Math.PI) * 128 + 128,0,255);
                    // byte green = (byte) Math.Clamp(Math.Sin(Math.Sqrt(xi * xi + yi * yi) / diviser * Math.PI)  * 128 + 128,0,255);
                    
                    byte alpha = 255;
                    SetColor(fb, x, y, red, green, blue, 255);
                }
            }
        }
    }

    private unsafe void SetColor(ILockedFramebuffer fb, int x, int y, byte r, byte g, byte b, byte a)
    {
        int bytesPerPixel = 4;
        int offset = y * fb.RowBytes + x * bytesPerPixel;

        byte* ptr = (byte*)fb.Address;

        ptr[offset + 0] = b; // Blue
        ptr[offset + 1] = g; // Green
        ptr[offset + 2] = r; // Red
        ptr[offset + 3] = a; // Alpha
    }
}