using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using OpenTK; //add "OpenTK" as NuGet reference
using OpenTK.Graphics.OpenGL4; //add "OpenTK" as NuGet reference
using OpenTK.Windowing.Common; //add "OpenTK" as NuGet reference
using OpenTK.Windowing.Desktop; //add "OpenTK" as NuGet reference
using OpenTK.Windowing.GraphicsLibraryFramework;

using var w = new GameWindow(
    new GameWindowSettings() { },
    new NativeWindowSettings()
    {
        API = ContextAPI.OpenGL,
        Flags = ContextFlags.ForwardCompatible | ContextFlags.Debug,
        SrgbCapable = true,
        ClientSize = new OpenTK.Mathematics.Vector2i(720, 480),
        Title = "ComGr",
        APIVersion = new Version(4, 1),
    });

int hProgram = 0;
int vaoTriangle = 0;
int vboTriangleIndices = 0;


int hTextureObject = 0;
int tiuIndex = 1;

Texture texture = TextureLoader.LoadFromFile("test.jpg");

List<SceneGraphNode> sceneGraph = new List<SceneGraphNode>();



sceneGraph.Add(new SceneGraphNode(Mesh.CreateCube(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0),
    new Vector3(1, 1, 0),
    new Vector3(1, 0, 1), new Vector3(1, 0, 0))));
sceneGraph.Add(new SceneGraphNode(Mesh.CreateCube(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1),
    new Vector3(0, 1, 0),
    new Vector3(1, 0, 1), new Vector3(0, 1, 1))));
sceneGraph.Add(new SceneGraphNode(Mesh.CreateCube(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1),
    new Vector3(1, 1, 0),
    new Vector3(0, 1, 0), new Vector3(0, 1, 1))));
sceneGraph[2].AddChild(new SceneGraphNode(Mesh.CreateSphere(20, new Vector3(1, 0, 0))),
    Matrix4x4.CreateTranslation(0, 3, 0));


List<float> vPositions = new List<float>();
List<float> vColors = new List<float>();
List<float> vTextelLocation = new List<float>();
List<int> vIndexes = new List<int>();
List<List<Matrix4x4>> Ms = new List<List<Matrix4x4>>();
List<int> indexOffsets = new List<int>();

foreach (SceneGraphNode sceneGraphNode in sceneGraph)
{
    List<Matrix4x4> tempM = new List<Matrix4x4>();
    tempM.Add(Matrix4x4.Identity);
    sceneGraphNode.getVertexIndexColorLists(vPositions, vColors, vIndexes, tempM, indexOffsets, vTextelLocation);
    Ms.Add(tempM);
}

float[] vertexPositions = vPositions.ToArray();
float[] vertexColors = vColors.ToArray();
float[] textelPositions = vTextelLocation.ToArray();
int[] indexes = vIndexes.ToArray();



Vector3 cameraPosition = new Vector3(0, 2, -4);
Vector3 cameraTarget = new Vector3(0, 0, 0);
Vector3 up = new Vector3(0, 1, 0);
Matrix4x4 V = Matrix4x4.CreateLookAtLeftHanded(cameraPosition, cameraTarget, up);

float zNear = 0.001f;
float zFar = 100f;
Matrix4x4 P = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(float.DegreesToRadians(90), 1, zNear, zFar);









w.Load += () =>
{
    GL.GenTextures(1, out hTextureObject);
    GL.BindTexture(TextureTarget.Texture2D, hTextureObject);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
    GL.PixelStore(PixelStoreParameter.UnpackAlignment,1);
    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Srgb8, texture.Width, texture.Height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, texture.Pixels);
    


    //set up opengl
    if (GLFW.ExtensionSupported("GL_KHR_debug"))
    {
        GL.Arb.DebugMessageCallback([DebuggerHidden](source​, type​, id​, severity​, length​, message​, userParam) =>
        {
            var msg = Marshal.PtrToStringAnsi(message, length);
            if (type == DebugType.DebugTypeError)
                throw new InvalidOperationException(msg);
            Console.WriteLine(msg);
        }, IntPtr.Zero);

        GL.Enable(EnableCap.DebugOutput);
        if (Debugger.IsAttached)
            GL.Enable(EnableCap.DebugOutputSynchronous);
    }

    GL.Enable(EnableCap.FramebufferSrgb);
    GL.ClearColor(0.5f, 0.5f, 0.5f, 0);
    GL.ClearDepth(1);
    GL.Enable(EnableCap.DepthTest);
    GL.DepthFunc(DepthFunction.Less);

    
    
    //load, compile and link shaders
    //see https://www.khronos.org/opengl/wiki/Vertex_Shader
    var VertexShaderSource = """
                             	#version 400 core

                             	in vec3 inPos;
                             	in vec3 inColor;
                             	in vec2 inTextelLocation;
                             	uniform mat4 inMatrix;
                             	out vec3 fromVSoutColor;
                             	out vec2 outTextelLocation;

                             	void main()
                             	{
                             		gl_Position = inMatrix * vec4(inPos, 1.0);
                             		fromVSoutColor = inColor;
                             		outTextelLocation = inTextelLocation;
                             	}
                             """;
    var hVertexShader = GL.CreateShader(ShaderType.VertexShader);
    GL.ShaderSource(hVertexShader, VertexShaderSource);
    GL.CompileShader(hVertexShader);
    GL.GetShader(hVertexShader, ShaderParameter.CompileStatus, out int status);
    if (status != (int)All.True)
        throw new Exception(GL.GetShaderInfoLog(hVertexShader));

    //see https://www.khronos.org/opengl/wiki/Fragment_Shader
    var FragmentShaderSource = """
                               	#version 400 core

                               	out vec4 outColor;
                               	in vec3 fromVSoutColor;
                               	in vec2 outTextelLocation;
                               	uniform sampler2D textureTest;
                               	
                               	void main()
                               	{
                               	    vec4 texelColor = texture(textureTest, outTextelLocation);
                               		outColor = texelColor;
                               	}
                               """;
    var hFragmentShader = GL.CreateShader(ShaderType.FragmentShader);
    GL.ShaderSource(hFragmentShader, FragmentShaderSource);
    GL.CompileShader(hFragmentShader);
    GL.GetShader(hFragmentShader, ShaderParameter.CompileStatus, out status);
    if (status != (int)All.True)
        throw new Exception(GL.GetShaderInfoLog(hFragmentShader));

    //link shaders to a program
    hProgram = GL.CreateProgram();
    GL.AttachShader(hProgram, hFragmentShader);
    GL.AttachShader(hProgram, hVertexShader);
    GL.LinkProgram(hProgram);
    GL.GetProgram(hProgram, GetProgramParameterName.LinkStatus, out status);
    if (status != (int)All.True)
        throw new Exception(GL.GetProgramInfoLog(hProgram));


    //upload vertex positions to a vbo
    var vboVertexPositions = GL.GenBuffer();
    GL.BindBuffer(BufferTarget.ArrayBuffer, vboVertexPositions);
    GL.BufferData(BufferTarget.ArrayBuffer, vertexPositions.Length * sizeof(float), vertexPositions,
        BufferUsageHint.StaticDraw);

    // upload model indices to a vbo
    vboTriangleIndices = GL.GenBuffer();
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboTriangleIndices);
    GL.BufferData(BufferTarget.ElementArrayBuffer, indexes.Length * sizeof(int), indexes, BufferUsageHint.StaticDraw);

    var vboColorIndices = GL.GenBuffer();
    GL.BindBuffer(BufferTarget.ArrayBuffer, vboColorIndices);
    GL.BufferData(BufferTarget.ArrayBuffer, vertexColors.Length * sizeof(float), vertexColors,
        BufferUsageHint.StaticDraw);
    
    var vboTextelPositions = GL.GenBuffer();
    GL.BindBuffer(BufferTarget.ArrayBuffer, vboTextelPositions);
    GL.BufferData(BufferTarget.ArrayBuffer, textelPositions.Length * sizeof(float), textelPositions,
        BufferUsageHint.StaticDraw);

    //set up a vao
    vaoTriangle = GL.GenVertexArray();
    GL.BindVertexArray(vaoTriangle);
    var posAttribIndex = GL.GetAttribLocation(hProgram, "inPos");
    if (posAttribIndex != -1)
    {
        GL.EnableVertexAttribArray(posAttribIndex);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vboVertexPositions);
        GL.VertexAttribPointer(posAttribIndex, 3, VertexAttribPointerType.Float, false, 0, 0);
    }


    var colorAttribIndex = GL.GetAttribLocation(hProgram, "inColor");
    if (colorAttribIndex != -1)
    {
        GL.EnableVertexAttribArray(colorAttribIndex);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vboColorIndices);
        GL.VertexAttribPointer(colorAttribIndex, 3, VertexAttribPointerType.Float, false, 0, 0);
    }

    var textelAttribIndex = GL.GetAttribLocation(hProgram, "inTextelLocation");
    if (textelAttribIndex != -1)
    {
        GL.EnableVertexAttribArray(textelAttribIndex);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vboTextelPositions);
        GL.VertexAttribPointer(textelAttribIndex, 2, VertexAttribPointerType.Float, false, 0, 0);
    }

    //check for errors during all previous calls
    var error = GL.GetError();
    if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
        throw new Exception(error.ToString());
};

double time = 0;
w.UpdateFrame += fea =>
{
    Ms[0][0] = Matrix4x4.CreateRotationX((float)time) * Matrix4x4.CreateTranslation(-2, 0, 0);
    Ms[1][0] = Matrix4x4.CreateRotationX((float)time / 2) * Matrix4x4.CreateTranslation(0, 0, 0);
    Ms[2][0] = Matrix4x4.CreateRotationX((float)time * 2) * Matrix4x4.CreateTranslation(2, 0, 0);

    time += fea.Time;
};

w.RenderFrame += fea =>
{
    
    
    //clear screen and z-buffer
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    //switch to our shader
    GL.UseProgram(hProgram);

    //render our model
    GL.ActiveTexture(TextureUnit.Texture0 + tiuIndex);
    GL.BindTexture(TextureTarget.Texture2D, hTextureObject);
    GL.Uniform1(GL.GetUniformLocation(hProgram, "textureTest"), tiuIndex);
    
    GL.BindVertexArray(vaoTriangle);
    GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboTriangleIndices);
    int index = 0;
    for (int i = 0; i < sceneGraph.Count; i++)
    {
        index = renderElements(sceneGraph[i],Ms[i][0], V*P, index);
    }

    //display
    w.SwapBuffers();

    var error = GL.GetError();
    if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
        throw new Exception(error.ToString());
};

w.FramebufferResize += rea => GL.Viewport(0, 0, rea.Width, rea.Height);
w.Run();

int renderElements(SceneGraphNode sceneGraphNode, Matrix4x4 M, Matrix4x4 PV, int index)
{
    
    Matrix4x4 MVP = M * PV;
    GL.UniformMatrix4(GL.GetUniformLocation(hProgram, "inMatrix"), 1, false, ref MVP.M11);
    GL.DrawElements(PrimitiveType.Triangles, sceneGraphNode.mesh.Tris.Count * 3, DrawElementsType.UnsignedInt,
        index * sizeof(uint));
    index += sceneGraphNode.mesh.Tris.Count * 3;
    foreach (var child in sceneGraphNode.children)
    {
       index = renderElements(child.child, child.transform * M, PV, index);
    }
    return index;
}

public class SceneGraphNode
{
    public List<(SceneGraphNode child, Matrix4x4 transform)> children =
        new List<(SceneGraphNode child, Matrix4x4 transform)>();

    public Mesh mesh;
    public int triCountOfSelfAndChildren;

    public SceneGraphNode(Mesh mesh)
    {
        this.mesh = mesh;
        triCountOfSelfAndChildren += mesh.Tris.Count;
    }

    public void AddChild(SceneGraphNode child, Matrix4x4 transform)
    {
        children.Add((child, transform));
        triCountOfSelfAndChildren += child.triCountOfSelfAndChildren;
    }

    public void getVertexIndexColorLists(List<float> vertexPositions, List<float> vertexColors,
        List<int> indexes, List<Matrix4x4> Ms, List<int> indexOffsets, List<float> textelLocations)
    {
        Debug.Assert(vertexPositions != null && vertexColors != null && indexes != null);
        int indexOfSet = vertexPositions.Count / 3;
        indexOffsets.Add(indexOfSet);


        foreach (Vertex vertex in mesh.Vertices)
        {
            vertexPositions.Add(vertex.Position.X);
            vertexPositions.Add(vertex.Position.Y);
            vertexPositions.Add(vertex.Position.Z);
            vertexColors.Add(vertex.Color.X);
            vertexColors.Add(vertex.Color.Y);
            vertexColors.Add(vertex.Color.Z);
            textelLocations.Add(vertex.TexCoord.X);
            textelLocations.Add(vertex.TexCoord.Y);
        }
        
        foreach (var tri in mesh.Tris)
        {
            indexes.Add(tri.A + indexOfSet);
            indexes.Add(tri.B + indexOfSet);
            indexes.Add(tri.C + indexOfSet);
        }

        foreach ((SceneGraphNode child, Matrix4x4 transform) in children)
        {
            Ms.Add(transform);
            child.getVertexIndexColorLists(vertexPositions, vertexColors,
                indexes, Ms, indexOffsets, textelLocations);
        }
    }
}