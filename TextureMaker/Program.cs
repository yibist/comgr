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
                    
                    byte blue = 255;
                    byte red = (byte)(x * 255/Width);
                    byte green = 110;

                    int xi = x % 50;
                    int yi = y % 50;
                    int r = x / 25;

                    
                      
                    
                    if (xi*xi + yi*yi < 50*50 )
                    {
                        blue = 0;
                        red = 0;
                        green = 0;
                    }
                    
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