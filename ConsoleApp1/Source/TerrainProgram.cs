using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Minecraft;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

public class TerrainProgram
{
    private static IWindow window;
    private static GL Gl;
    private static IKeyboard primaryKeyboard;

    private static BufferObject<float> Vbo;
    private static VertexArrayObject<float, uint> Vao;

    private static uint voxelTexture;
    private static uint computeShader, voxelShaderProgram, renderShaderProgram;

    private static FPSCamera fps_camera = new FPSCamera();
    private static OrbitCamera orbit_camera = new OrbitCamera();
    private static Camera camera = fps_camera;

    private static float time;

    private static Vector3 chunkOffset;

    private static bool wireframeMode = false;

    private enum ViewMode
    {
        FirstPerson = 0,
        Orbit = 1
    }

    private static ViewMode viewMode = ViewMode.FirstPerson;

    private static float CameraZoom = 45f;
    private static float CameraRadius = 10f;

    // public static void Main(string[] args)
    public static unsafe void Start()
    {
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(1280, 720);
        options.Title = "Voxel Terrain Generation";
        var flags = ContextFlags.ForwardCompatible;

        flags |= ContextFlags.Debug;

        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, flags, new APIVersion(4, 6));
        options.PreferredDepthBufferBits = 24;
        window = Window.Create(options);

        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;
        window.FramebufferResize += OnFramebufferResize;
        window.Closing += OnClose;

        window.Run();
        window.Dispose();
    }

    private static unsafe void OnLoad()
    {
        try
        {
            IInputContext input = window.CreateInput();
            primaryKeyboard = input.Keyboards.FirstOrDefault();
            if (primaryKeyboard != null)
            {
                primaryKeyboard.KeyDown += KeyDown;
            }
            for (int i = 0; i < input.Mice.Count; i++)
            {
                input.Mice[i].Cursor.CursorMode = CursorMode.Raw;
                input.Mice[i].MouseMove += OnMouseMove;
                input.Mice[i].Scroll += OnMouseWheel;
            }

            Gl = GL.GetApi(window);

            // Chargement et compilation du compute shader
            computeShader = Gl.CreateShader(ShaderType.ComputeShader);
            var computeSource = File.ReadAllText("../../../Assets/Shaders/VoxelComputeShader.glsl");
            Gl.ShaderSource(computeShader, computeSource);
            Gl.CompileShader(computeShader);
            CheckShaderCompileError(computeShader);

            voxelShaderProgram = Gl.CreateProgram();
            Gl.AttachShader(voxelShaderProgram, computeShader);
            Gl.LinkProgram(voxelShaderProgram);
            CheckProgramLinkError(voxelShaderProgram);

            // Création de la texture 3D pour stocker les voxels
            voxelTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture3D, voxelTexture);
            Gl.TexImage3D(TextureTarget.Texture3D, 0, InternalFormat.Rgba8, 256, 256, 256, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            Gl.BindTexture(TextureTarget.Texture3D, 0);

            // Chargement et compilation des shaders de rendu
            var vertexShader = Gl.CreateShader(ShaderType.VertexShader);
            var fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
            var vertexSource = File.ReadAllText("../../../Assets/Shaders/VoxelVertexShader.glsl");
            var fragmentSource = File.ReadAllText("../../../Assets/Shaders/VoxelFragmentShader.glsl");
            Gl.ShaderSource(vertexShader, vertexSource);
            Gl.ShaderSource(fragmentShader, fragmentSource);
            Gl.CompileShader(vertexShader);
            Gl.CompileShader(fragmentShader);
            CheckShaderCompileError(vertexShader);
            CheckShaderCompileError(fragmentShader);

            renderShaderProgram = Gl.CreateProgram();
            Gl.AttachShader(renderShaderProgram, vertexShader);
            Gl.AttachShader(renderShaderProgram, fragmentShader);
            Gl.LinkProgram(renderShaderProgram);
            CheckProgramLinkError(renderShaderProgram);

            Gl.UseProgram(renderShaderProgram);
            Gl.Uniform1(Gl.GetUniformLocation(renderShaderProgram, "voxelTexture"), 0);

            // Définir les vertices pour un cube (ou toute autre géométrie de test)
            float[] quadVertices = {
                // Positions        // TexCoords
                -1.0f,  1.0f,  1.0f,  0.0f, 1.0f, 1.0f,
                -1.0f, -1.0f,  1.0f,  0.0f, 0.0f, 1.0f,
                 1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
                -1.0f,  1.0f,  1.0f,  0.0f, 1.0f, 1.0f,
                 1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
                 1.0f,  1.0f,  1.0f,  1.0f, 1.0f, 1.0f,
                // Repeat for all six faces of the cube...
            };

            // Initialisation du VAO et du VBO pour dessiner un cube
            Vbo = new BufferObject<float>(Gl, quadVertices, BufferTargetARB.ArrayBuffer);
            Vao = new VertexArrayObject<float, uint>(Gl, Vbo);
            Vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 6, 0);
            Vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 6, 3);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erreur lors du chargement : {e.Message}");
        }
    }

    private static unsafe void OnUpdate(double deltaTime)
    {
        var moveSpeed = 6f * (float)deltaTime;

        if (primaryKeyboard.IsKeyPressed(Key.W))
        {
            camera.Position += camera.Front * moveSpeed;
        }
        if (primaryKeyboard.IsKeyPressed(Key.S))
        {
            camera.Position -= camera.Front * moveSpeed;
        }
        if (primaryKeyboard.IsKeyPressed(Key.A))
        {
            camera.Position -= Vector3.Normalize(Vector3.Cross(camera.Front, World.Up)) * moveSpeed;
        }
        if (primaryKeyboard.IsKeyPressed(Key.D))
        {
            camera.Position += Vector3.Normalize(Vector3.Cross(camera.Front, World.Up)) * moveSpeed;
        }
        if (primaryKeyboard.IsKeyPressed(Key.E))
        {
            Vector3 right = Vector3.Normalize(Vector3.Cross(camera.Front, World.Up));
            camera.Position += Vector3.Normalize(Vector3.Cross(right, camera.Front)) * moveSpeed;
        }
        if (primaryKeyboard.IsKeyPressed(Key.Q))
        {
            Vector3 right = Vector3.Normalize(Vector3.Cross(camera.Front, World.Up));
            camera.Position -= Vector3.Normalize(Vector3.Cross(right, camera.Front)) * moveSpeed;
        }

        if (primaryKeyboard.IsKeyPressed(Key.Up))
        {
            camera.SetFov(0.5f);
        }
        else if (primaryKeyboard.IsKeyPressed(Key.Down))
        {
            camera.SetFov(-0.5f);
        }
    }

    private static unsafe void OnRender(double deltaTime)
    {
        time += (float)deltaTime;
        chunkOffset = new Vector3(0.0f, 0.0f, 0.0f);
        orbit_camera.Rotate(camera.CameraYaw, camera.CameraPitch);
        orbit_camera.SetRadius(CameraRadius);

        Gl.Enable(EnableCap.DepthTest);
        Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        // Exécution du compute shader pour générer les voxels
        Gl.UseProgram(voxelShaderProgram);
        Gl.BindImageTexture(0, voxelTexture, 0, false, 0, BufferAccessARB.WriteOnly, InternalFormat.Rgba8);
        Gl.Uniform1(Gl.GetUniformLocation(voxelShaderProgram, "time"), time);
        Gl.Uniform3(Gl.GetUniformLocation(voxelShaderProgram, "offset"), chunkOffset);
        Gl.DispatchCompute(256 / 8, 256 / 8, 256 / 8);
        Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

        // Rendu des voxels
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Gl.UseProgram(renderShaderProgram);
        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.Texture3D, voxelTexture);
        Vao.Bind();
        Gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }

    private static void OnFramebufferResize(Vector2D<int> newSize)
    {
        Gl.Viewport(newSize);
    }

    private static unsafe void OnMouseMove(IMouse mouse, Vector2 position)
    {
        camera.OnMouseMove(mouse, position);
    }

    private static unsafe void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
    {
        switch (viewMode)
        {
            case ViewMode.FirstPerson:
                camera.SetFov(scrollWheel.Y);
                break;
            case ViewMode.Orbit:
                CameraRadius -= scrollWheel.Y * 2;
                CameraRadius = Math.Clamp(CameraRadius, 0.5f, 99);
                break;
        }
    }

    private static void OnClose()
    {
        Dispose();
    }

    private static void Dispose()
    {
        Vbo.Dispose();
        Vao.Dispose();
    }

    private static void CheckShaderCompileError(uint shader)
    {
        Gl.GetShader(shader, ShaderParameterName.CompileStatus, out var success);
        if (success == 0)
        {
            var infoLog = Gl.GetShaderInfoLog(shader);
            Console.WriteLine($"Erreur de compilation du shader:\n{infoLog}");
        }
    }

    private static void CheckProgramLinkError(uint program)
    {
        Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var success);
        if (success == 0)
        {
            var infoLog = Gl.GetProgramInfoLog(program);
            Console.WriteLine($"Erreur de linking du shader program:\n{infoLog}");
        }
    }

    private static void KeyDown(IKeyboard keyboard, Key key, int arg3)
    {
        if (key == Key.Escape)
        {
            window.Close();
        }

        if (key == Key.Z)
        {
            wireframeMode = !wireframeMode;
            if (wireframeMode)
            {
                Gl.PolygonMode(GLEnum.FrontAndBack, PolygonMode.Line);
            }
            else
            {
                Gl.PolygonMode(GLEnum.FrontAndBack, PolygonMode.Fill);
            }
        }

        if (key == Key.F5)
        {
            if (viewMode == ViewMode.FirstPerson)
            {
                viewMode = ViewMode.Orbit;
                camera = orbit_camera;
            }
            else if (viewMode == ViewMode.Orbit)
            {
                viewMode = ViewMode.FirstPerson;
                camera = fps_camera;
            }
        }

        if (key == Key.F11)
        {
            if (window.WindowState == WindowState.Fullscreen)
            {
                window.WindowState = WindowState.Normal;
            }
            else
            {
                window.WindowState = WindowState.Fullscreen;
            }
        }
    }
}