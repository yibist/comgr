using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;

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
    public override void Initialize()
    {
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

class Renderer : Control
{
    private double x = 0;
    private double speed = 4;
    private readonly DispatcherTimer timer;

    public Renderer()
    {
        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        timer.Tick += (_, _) => Step();
        timer.Start();
    }

    private void Step()
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        double triangleWidth = h / 2; 

        x += speed;

        if (x > w - triangleWidth) { x = w - triangleWidth; speed = -speed; }
        if (x < 0)                 { x = 0;                 speed = -speed; }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var h = Bounds.Height;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(5 + x ,10), true);
            ctx.LineTo(new Point(5+ x,40));     
            ctx.LineTo(new Point(100+ x, 40)); 
            ctx.EndFigure(true);
        }

        context.DrawGeometry(null, new Pen(Brushes.White, 1), geometry);
    }
}

class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Triangle";
        Width = 400;
        Height = 400;
        Background = Brushes.Black;
        Content = new Renderer();
    }
}