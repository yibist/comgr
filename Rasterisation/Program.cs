using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System.Numerics;
using Avalonia.Controls.ApplicationLifetimes;
using Vector = Avalonia.Vector;

//https://chat.deepseek.com/share/urrfadium1wwjfqfbe

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
    private WriteableBitmap bitmap;
    private Vector3[,] colors;
    // private Mesh mesh = Mesh.CreateCube(new Vector3(1,0,0),new Vector3(0,1,0),new Vector3(0,0,1),new Vector3(1,1,0),new Vector3(1,0,1),new Vector3(0,1,1));
    // private Mesh mesh = Mesh.CreateCube(new Vector3(1,0,0),new Vector3(1,0,0),new Vector3(1,0,0),new Vector3(1,0,0),new Vector3(1,0,0),new Vector3(1,0,0));
    private Mesh mesh = Mesh.CreateSphere(3,new Vector3(1,0,0));
    private const int imageWidth = 800;
    private const int imageHeight = 800;
    private int calculationThreads;
    private Barrier frameBarrier;
    private double cameraDistance = 100;
    
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private TimeSpan lastFrameTime;
    private double deltaTime;
    private float timePassed = 0f;
    
    private Matrix4x4 M;
    private Matrix4x4 MNormal;
    private Vector3 cameraPosition = new Vector3(0, 2, -4);
    private Vector3 cameraTarget = new Vector3(0, 0, 0);
    private Vector3 up = new Vector3(0, 1, 0);
    private Matrix4x4 V;
    
    private float zNear = 0.001f;
    private float zFar = 100f;
    private Matrix4x4 P;
    
    Matrix4x4 MVP;

    private Vector3 lightSourceLocation = new Vector3(200,500,-200);
    
    private Vertex[,] vertices;
    
    public MainWindow()
    {
        calculationThreads = Math.Max(1, Environment.ProcessorCount - 2);
        // calculationThreads = 1;
        
        frameBarrier = new Barrier(calculationThreads + 1);
        Title = "comgr";
        Width = imageWidth;
        Height = imageHeight;

        V = Matrix4x4.CreateLookAtLeftHanded(cameraPosition, cameraTarget, up);
        P = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(float.DegreesToRadians(90), 1, zNear, zFar);
        imageControl = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        Content = imageControl;

        colors = new Vector3[imageHeight, imageWidth];

        bitmap = new WriteableBitmap(
            new PixelSize(imageWidth, imageHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        imageControl.Source = bitmap;
        vertices = new Vertex[mesh.Tris.Count,3];
        for (int i = 0; i < calculationThreads; i++)
        {
            int threadId = i;
            var t = new Thread(() => WorkerLoop(threadId))
            {
                IsBackground = true,
            };
            t.Start();
        }
        
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1) };
        timer.Tick += (s, e) => RenderLoop();
        timer.Start();
    }
    private unsafe void RenderLoop()
    {
        var now = _stopwatch.Elapsed;
        deltaTime = (now - lastFrameTime).TotalSeconds;
        lastFrameTime = now;


        using (var fb = bitmap.Lock())
        {
            var ptr = (byte*)fb.Address;
            var stride = fb.RowBytes;

            for (int y = 0; y < imageHeight; y++)
            {
                var rowPtr = ptr + y * stride;

                for (int x = 0; x < imageWidth; x++)
                {
                    Vector3 vec = colors[y, x];

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
        imageControl.InvalidateVisual();
        
        frameBarrier.SignalAndWait();
        frameBarrier.SignalAndWait();
        Console.WriteLine(deltaTime);
        timePassed += (float) deltaTime;
        M = Matrix4x4.CreateRotationY(timePassed);
        Matrix4x4.Invert(M, out Matrix4x4 inv);

        MNormal = Matrix4x4.Multiply(Matrix4x4.Transpose(inv), M.GetDeterminant());
        MVP = M * V * P;
        
        frameBarrier.SignalAndWait();
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


    private void WorkerLoop(int threadId)
    {
        int rowsPerThread = imageHeight / calculationThreads;
        int startRow = threadId * rowsPerThread;
        int endRow = (threadId == calculationThreads - 1) ? imageHeight : startRow + rowsPerThread;
        int start = mesh.Tris.Count * threadId / calculationThreads;
        int end   = mesh.Tris.Count * (threadId + 1) / calculationThreads;

        Random random = new Random();

        while (true)
        {
            for (int i = start; i < end; i++)
            {
                var tri = mesh.Tris[i];
                Vertex v1 =mesh.Vertices[tri.A];
                Vertex v2 =mesh.Vertices[tri.B];
                Vertex v3 =mesh.Vertices[tri.C];
                
            

                v1 = VertexShader(v1);
                v2 = VertexShader(v2);
                v3 = VertexShader(v3);
                vertices[i,0] = v1;
                vertices[i,1] = v2; 
                vertices[i,2] = v3;
            }
            frameBarrier.SignalAndWait();
            
            
            
            for (int y = startRow; y < endRow; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    colors[y, x] = ComputeColor(x,y);
                }
            }
            frameBarrier.SignalAndWait();
            frameBarrier.SignalAndWait();
        }
    }

    private Vector3 ComputeColor(float x, float y)
    {
        x = (x / imageWidth) * 2 - 1;
        y = 1 - (y / imageHeight) * 2;
        float closestDistance = float.MaxValue;
        for (int i = 0; i<vertices.GetLength(0); i++)
        {
            var v1 = vertices[i,0];
            var v2 = vertices[i,1];
            var v3 = vertices[i,2];



            if (v1.Position.W <= 0 || v2.Position.W <= 0 || v3.Position.W <= 0)
                continue;

            Vector4 a = v1.Position;
            Vector4 b = v2.Position;
            Vector4 c = v3.Position;

            Vector2 A = new Vector2(a.X / a.W, a.Y / a.W);
            Vector2 B = new Vector2(b.X / b.W, b.Y / b.W);
            Vector2 C = new Vector2(c.X / c.W, c.Y / c.W);
            
            var AB = B - A;
            var AC = C - A;
            
            Vector2 A2 = new Vector2(A.X, A.Y);
            Vector2 AB2 = new Vector2(AB.X, AB.Y);
            Vector2 AC2 = new Vector2(AC.X, AC.Y);

            float det = AB2.X * AC2.Y - AC2.X * AB2.Y;
            if (det < 0f) continue; // this does backface culling
            float invDet = 1.0f / det;

            Vector2 P2 = new Vector2(x - A2.X, y - A2.Y);

            float L2 = ( AC2.Y * P2.X - AC2.X * P2.Y) * invDet;
            float L3 = (-AB2.Y * P2.X + AB2.X * P2.Y) * invDet;
            float L1 = 1.0f - L2 - L3;

            
            if ((L1 >= 0 && L2 >= 0 && L3 >= 0) || (L1 <= 0 && L2 <= 0 && L3 <= 0))
            {

                float bw1 = L1 * 1.0f / v1.Position.W;
                float bw2 = L2 * 1.0f / v2.Position.W;
                float bw3 = L3 * 1.0f / v3.Position.W;
                float bwSum = bw1 + bw2 + bw3;
                Vector3 fragWorldPos = (v1.WorldCoordinates * bw1
                                        + v2.WorldCoordinates * bw2
                                        + v3.WorldCoordinates * bw3) / bwSum;

                float distance = (cameraPosition - fragWorldPos).Length();
                if (closestDistance < distance)
                {
                    continue;
                }
                closestDistance = distance;
                
                Vector3 N = Vector3.Normalize((v1.Normal * bw1 + v2.Normal * bw2 + v3.Normal * bw3) / bwSum);
                
                Vector3 toLight = lightSourceLocation - fragWorldPos;
                Vector3 L = Vector3.Normalize(toLight);

                float lambertStrength = MathF.Max(0f, Vector3.Dot(N, L));

                Vector3 baseColor = v1.Color * L1 + v2.Color * L2 + v3.Color * L3;
                Vector3 diffuse = baseColor * lambertStrength;
                
                

                Vector3 V = Vector3.Normalize(cameraPosition - fragWorldPos);

                Vector3 R = Vector3.Reflect(-L, N);
                float phongStrength = MathF.Max(0f, Vector3.Dot(R, V));
                float specular = MathF.Pow(phongStrength, 100);

                specular *= (lambertStrength > 0f) ? 1f : 0f;

                Vector3 specularColor = new Vector3(1,1,1) * 100 * specular;

                return diffuse + specularColor;
            }
        }
        return Vector3.Zero;
        
    }


    Vertex VertexShader(Vertex v)
    {
        Vertex res = new Vertex(Vector4.Transform(v.Position, MVP), Vector4.Transform(v.Position, M).AsVector3(),
            v.Color, v.TexCoord, Vector3.Transform(v.Normal, MNormal));
        return res;
    }
}


