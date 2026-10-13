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
//https://chat.deepseek.com/share/fkcc01lo9z8x09x2hj
//https://chat.deepseek.com/share/jpgt4ajk9udzdgx3oh

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
    private Texture t1 = TextureLoader.LoadFromFile("test.jpg");
    private Texture t2 = TextureLoader.LoadFromFile("1222.jpg");

    private const int imageWidth = 800;
    private const int imageHeight = 800;
    private int calculationThreads = 8;

    private float FOV = (float)(36.0f * (Math.PI / 180f));
    private Vector3 eye = new Vector3(0, 0, 0);
    private Vector3 up = new Vector3(0, 1, 0);
    private Vector3 lookAt = new Vector3(0, 0, 6);
    private float p = 0.5f;
    private int bounceDepth = int.MaxValue;


    public MainWindow()
    {
        calculationThreads = Math.Max(1, Environment.ProcessorCount - 2);
        initScene();
        Title = "comgr";
        Width = imageWidth;
        Height = imageHeight;

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

        for (int i = 0; i < calculationThreads; i++)
        {
            int threadId = i;
            Task.Run(() => CalculationWorker(threadId));
        }

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        timer.Tick += (s, e) => RenderFrame();
        timer.Start();
    }

    private unsafe void RenderFrame()
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

    private List<Sphere> scene = new List<Sphere>();

    private void initScene()
    {
        scene.Add(new Sphere(new Vector3(0, 0, 1000), 250, new Vector3(1, 0, 0), new Vector3(0, 0, 0),
            new Vector3(0, 0, 0), t1));
        
        scene.Add(new Sphere(new Vector3(6, 0, 50), 5, new Vector3(0, 0, 0), new Vector3(0, 0, 0),
            new Vector3(0, 0, 0), null, 1.1f, true));
        scene.Add(new Sphere(new Vector3(-6, 0, 50), 5, new Vector3(0, 0, 0), new Vector3(0, 0, 0),
            new Vector3(0, 0, 0), null, 1.5f, true));

        
        scene.Add(new Sphere(new Vector3(0, 0, -1050), 1000, new Vector3(1, 1, 1), new Vector3(1, 1, 1)*10,
            new Vector3(0, 0, 0)));
        
        scene.Add(new Sphere(new Vector3(1100, 1100, 0), 1000, new Vector3(1, 1, 1), new Vector3(1, 1, 1)*10,
            new Vector3(0, 0, 0)));
    }


    private void CalculationWorker(int threadId)
    {
        int rowsPerThread = imageHeight / calculationThreads;
        int startRow = threadId * rowsPerThread;
        int endRow = (threadId == calculationThreads - 1) ? imageHeight : startRow + rowsPerThread;
        int rays = 1;
        Random random = new Random();

        while (true)
        {
            for (int y = startRow; y < endRow; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    (Vector3 origin, Vector3 direction) = CreateEyeRay(eye, lookAt, FOV, new Vector2(x, y));
                    colors[y, x] += (ComputeColor(origin, direction, 0, random) - colors[y, x]) / rays;
                }
            }

            rays++;
        }
    }

    private (Vector3 origin, Vector3 direction) CreateEyeRay(Vector3 eye, Vector3 lookAt, float FOV, Vector2 Pixel)
    {
        float x = Pixel.X / imageWidth * 2 - 1;
        float y = Pixel.Y / imageHeight * 2 - 1;

        Vector3 f = Vector3.Normalize(lookAt - eye);
        Vector3 r = Vector3.Normalize(Vector3.Cross(up, f));
        Vector3 u = Vector3.Cross(r, f);

        float lambda1 = (float)Math.Tan(FOV / 2);
        Vector3 d = f + x * r * lambda1 + y * u * lambda1;
        return (eye, Vector3.Normalize(d));
    }

    private (Vector3? hitPoint, Sphere? shape) FindClosestHitPoint(Vector3 origin, Vector3 direction)
    {
        List<float> hitLambdas = new List<float>();
        List<Sphere> hitSpheres = new List<Sphere>();
        foreach (var sphere in scene)
        {
            Vector3 CE = (origin - sphere.location);
            float b = 2 * Vector3.Dot(CE, direction);
            float c = Vector3.Dot(CE, CE) - sphere.radius * sphere.radius;

            float underRoot = b * b - 4 * c;
            if (underRoot < 0)
            {
                continue;
            }

            float lambda1 = (float)((-b + Math.Sqrt(underRoot)) / 2);
            float lambda2 = (float)((-b - Math.Sqrt(underRoot)) / 2);

            if (lambda1 < 0 && lambda2 < 0) continue;
            if (lambda1 < 0)
            {
                hitLambdas.Add(lambda2);
            }
            else if (lambda2 < 0)
            {
                hitLambdas.Add(lambda1);
            }
            else
            {
                hitLambdas.Add(Math.Min(lambda1, lambda2));
            }

            hitSpheres.Add(sphere);
        }

        if (hitLambdas.Count == 0) return (null, null);
        return (origin + direction * hitLambdas.Min(), hitSpheres[hitLambdas.IndexOf(hitLambdas.Min())]);
    }

    Vector3 ComputeColor(Vector3 origin, Vector3 direction, int depth, Random random, float speedInMedium = 1)
    {
        (Vector3? hp, Sphere? s) = FindClosestHitPoint(origin, direction);


        if (!hp.HasValue && !s.HasValue) return new Vector3(0, 0, 0);

        Vector3 hitPoint = (Vector3)hp;
        Sphere sphere = (Sphere)s;


        if (random.NextDouble() < p || depth > bounceDepth)
        {
            return sphere.emission;
        }


        Vector3 surfaceNormal = Vector3.Normalize(hitPoint - sphere.location);
        float dot;
        Vector3 sampleDir;

        if (sphere.isSeetrough && random.NextDouble() >
            FresnelReflectance(direction, surfaceNormal, speedInMedium, sphere.speedInMedium))
        {
            Vector3? refractanceDirection = Refract(direction, surfaceNormal, speedInMedium, sphere.speedInMedium);

            if (!refractanceDirection.HasValue)
            {
                return sphere.emission; // should probably not ignore internal reflection
            }

            Vector3 refrOrigin = hitPoint + refractanceDirection.Value * 1e-3f;

            float newSpeed = (speedInMedium == sphere.speedInMedium) ? 1f : sphere.speedInMedium; // quick and dirty way

            return ComputeColor(refrOrigin, refractanceDirection.Value, depth + 1, random, newSpeed);
            
        }
        
        
        while (Vector3.Distance(hitPoint, sphere.location) <= sphere.radius)
        {
            hitPoint += surfaceNormal * 1e-3f;
        }

        float x;
        float y;
        float z;

        while (true)
        {
            x = (float)random.NextDouble() * 2 - 1;
            y = (float)random.NextDouble() * 2 - 1;
            z = (float)random.NextDouble() * 2 - 1;
            sampleDir = new Vector3(x, y, z);
            if (sampleDir.Length() < 1)
            {
                sampleDir = Vector3.Normalize(sampleDir);
                break;
            }
        }

        dot = Vector3.Dot(sampleDir, surfaceNormal);
        if (dot < 0)
        {
            sampleDir = -sampleDir;
            dot = -dot;
        }

        return sphere.emission + (float)(2 * Math.PI / (1.0f - p)) * dot
                                                                   * BRDF(direction, sampleDir, surfaceNormal, sphere,
                                                                       hitPoint)
                                                                   * ComputeColor(hitPoint, sampleDir, depth + 1,
                                                                       random);
    }

    Vector3 BRDF(Vector3 incomingDirection, Vector3 outgoingDirection, Vector3 surfaceNormal, Sphere hitObject,
        Vector3 hitPoint)
    {
        Vector3 d = Vector3.Reflect(outgoingDirection, surfaceNormal);
        double epsilon = 0.05;
        if (Vector3.Dot(d, incomingDirection) > 1.0 - epsilon)
        {
            return hitObject.Diffuse(hitPoint) * (float)(1.0 / Math.PI) + hitObject.reflectance * 10;
        }

        return hitObject.Diffuse(hitPoint) * (float)(1.0 / Math.PI);
    }

    
    public static Vector3? Refract(Vector3 direction, Vector3 surfaceNormal, float speedIn, float speedTo)
    {
        direction = Vector3.Normalize(direction);
        surfaceNormal = Vector3.Normalize(surfaceNormal);

        float eta = speedIn / speedTo;
        float cosI = -Vector3.Dot(surfaceNormal, direction);

        if (cosI < 0f)
        {
            surfaceNormal = -surfaceNormal;
            cosI = -cosI;
            eta = speedTo / speedIn;
        }

        float sin2T = eta * eta * (1f - cosI * cosI);

        if (sin2T > 1f)
            return null;

        float cosT = MathF.Sqrt(1f - sin2T);
        return eta * direction + (eta * cosI - cosT) * surfaceNormal;
    }

    public static float FresnelReflectance(Vector3 direction, Vector3 surfaceNormal, float speedIn, float speedTo)
    {
        direction = Vector3.Normalize(direction);
        surfaceNormal = Vector3.Normalize(surfaceNormal);

        float cosI = -Vector3.Dot(surfaceNormal, direction);
        if (cosI < 0f)
        {
            surfaceNormal = -surfaceNormal;
            cosI = -cosI;
            (speedIn, speedTo) = (speedTo, speedIn);
        }

        cosI = Math.Clamp(cosI, 0f, 1f);

        float r0 = (speedIn - speedTo) / (speedIn + speedTo);
        r0 *= r0;

        float oneMinusCos = 1f - cosI;
        float oneMinusCos5 = oneMinusCos * oneMinusCos;
        oneMinusCos5 *= oneMinusCos5 * oneMinusCos;

        return r0 + (1f - r0) * oneMinusCos5;
    }
}


struct Sphere
{
    public Vector3 location;
    public float radius;
    public float speedInMedium;
    public Vector3 diffuse;
    public Vector3 emission;
    public Vector3 reflectance;
    public Texture? texture;
    public bool isSeetrough;

    public Sphere(Vector3 location, float radius, Vector3 diffuse, Vector3 emission, Vector3 reflectance,
        Texture? texture = null, float speedInMedium = 1f, bool isSeetrough = false)
    {
        this.location = location;
        this.radius = radius;
        this.diffuse = diffuse;
        this.emission = emission;
        this.reflectance = reflectance;
        this.texture = texture;
        this.speedInMedium = speedInMedium;
        this.isSeetrough = isSeetrough;
    }

    private Vector2 GetUV(Vector3 hitPoint)
    {
        Vector3 d = Vector3.Normalize(hitPoint - location);

        float u = 0.5f + MathF.Atan2(d.Z, d.X) / (2f * MathF.PI);
        float v = 0.5f - MathF.Asin(d.Y) / MathF.PI;

        return new Vector2(u, v);
    }

    public Vector3 Diffuse(Vector3 hitPoint)
    {
        if (texture == null)
        {
            return diffuse;
        }

        Vector2 uv = GetUV(hitPoint);
        return texture.Sample(uv.X, uv.Y);
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

        int x = (int)(u * Width);
        int y = (int)(v * Height);
        if (x >= Width) x = Width - 1;
        if (y >= Height) y = Height - 1;
        return Pixels[y * Width + x];
    }
}