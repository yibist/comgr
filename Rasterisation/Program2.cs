using System;
using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Threading;



public class App2 : Application
{
    public override void Initialize()
    {
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow2();
        }
        base.OnFrameworkInitializationCompleted();
    }
}

class Renderer : Control
{
    private readonly DispatcherTimer timer;
    private double cameraDistance = 100;
    private Matrix4x4 m = Matrix4x4.CreateRotationY(0);
    private double timePassed = 0;
    private Mesh mesh =
        Mesh.CreateCube(Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero);

    private readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private TimeSpan lastTime;

    public Renderer()
    {
        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        timer.Tick += (_, _) => Step();
        lastTime = stopwatch.Elapsed;
        timer.Start();
    }

    private void Step()
    {
        var now = stopwatch.Elapsed;
        var delta = (float)(now - lastTime).TotalSeconds;
        lastTime = now;

        var w = Bounds.Width;
        var h = Bounds.Height;
        m = Matrix4x4.CreateRotationY((float)timePassed);
        timePassed += delta;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var h = Bounds.Height;
        var w = Bounds.Width;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            foreach (var t in mesh.Tris)
            {
                
                
                Vertex v1 =mesh.Vertices[t.A];
                Vertex v2 =mesh.Vertices[t.B];
                Vertex v3 =mesh.Vertices[t.C];

                v1 = VertexShader(v1);
                v2 = VertexShader(v2);
                v3 = VertexShader(v3);


                v1 = v1 * (1 / v1.Position.W);
                v2 = v2 * (1 / v2.Position.W);
                v3 = v3 * (1 / v3.Position.W);

                ctx.BeginFigure(
                    new Point(w / 2 + cameraDistance * (v1.Position.X), h / 2 + cameraDistance * (v1.Position.Y)), true);
                ctx.LineTo(new Point(w / 2 + cameraDistance * (v2.Position.X), h / 2 + cameraDistance * (v2.Position.Y)), true);
                ctx.LineTo(new Point(w / 2 + cameraDistance * (v3.Position.X), h / 2 + cameraDistance * (v3.Position.Y)), true);


                ctx.EndFigure(true);
                context.DrawGeometry(null, new Pen(Brushes.White, 1), geometry);
            }
        }
    }

    Vertex VertexShader(Vertex v)
    {
        Vector4 position = Vector4.Transform(v.Position, m);
        position = new Vector4(position.X, position.Y, 0, position.Z + 4);
        Vertex res = new Vertex(position, v.WorldCoordinates,
            v.Color, v.TexCoord, v.Normal);
        return res;
    }
}


class MainWindow2 : Window
{
    public MainWindow2()
    {
        Title = "Triangle";
        Width = 400;
        Height = 400;
        Background = Brushes.Black;
        Content = new Renderer();
    }
}