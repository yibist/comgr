using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System.Numerics;
using Avalonia.Controls.ApplicationLifetimes;
using Vector = Avalonia.Vector;

class Program
{
    static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

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
    private Image imageControl;
    private WriteableBitmap? bitmap;
    private Vector3[,]? vectors;

    private const int imageWidth = 500;
    private const int imageHeight = 500;
    private const int calculationThreads = 1;

    public MainWindow()
    {
        Title = "Multithreaded Image Generation";
        Width = 500;
        Height = 500;

        imageControl = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        Content = imageControl;

        vectors = new Vector3[imageHeight, imageWidth];

        for (int i = 0; i < calculationThreads; i++)
        {
            int threadId = i;
            Task.Run(() => CalculationWorker(threadId));
        }

        Task.Run(() => RenderWorker());

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        timer.Tick += (s, e) =>
        {
            if (bitmap != null)
            {
                imageControl.Source = bitmap;
                InvalidateVisual();
            }
        };
        timer.Start();
    }

    private void CalculationWorker(int threadId)
    {
        var random = new Random(threadId * 1337 + Environment.TickCount);

        int rowsPerThread = imageHeight / calculationThreads;
        int startRow = threadId * rowsPerThread;
        int endRow = (threadId == calculationThreads - 1) ? imageHeight : startRow + rowsPerThread;


        while (true)
        {
            for (int y = startRow; y < endRow; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    vectors[y, x] = new Vector3(1, 1, 1);
                }
            }

            Thread.Sleep(16);
        }
    }

    private unsafe void RenderWorker()
    {
        bitmap = new WriteableBitmap(
            new PixelSize(imageWidth, imageHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        while (true)
        {
            using (var fb = bitmap.Lock())
            {
                var ptr = (byte*)fb.Address;
                var stride = fb.RowBytes;

                for (int y = 0; y < imageHeight; y++)
                {
                    var rowPtr = ptr + y * stride;

                    for (int x = 0; x < imageWidth; x++)
                    {
                        Vector3 vec = vectors[y, x];

                        byte r = LinearToSRGB(vec.X);
                        byte g = LinearToSRGB(vec.Y);
                        byte b = LinearToSRGB(vec.Z);

                        int pixelOffset = x * 4;
                        rowPtr[pixelOffset + 0] = b;
                        rowPtr[pixelOffset + 1] = g;
                        rowPtr[pixelOffset + 2] = r;
                        rowPtr[pixelOffset + 3] = 255;
                    }
                }
            }

            Thread.Sleep(1000);
        }
    }

    private static byte LinearToSRGB(float linear)
    {
        linear = Math.Max(0, Math.Min(1, linear));

        float srgb;
        if (linear <= 0.0031308f)
        {
            srgb = linear * 12.92f;
        }
        else
        {
            srgb = 1.055f * (float)Math.Pow(linear, 1.0 / 2.4f) - 0.055f;
        }

        return (byte)(srgb * 255 + 0.5f);
    }
}