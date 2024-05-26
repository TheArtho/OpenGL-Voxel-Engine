using System.Numerics;
using System.Runtime.InteropServices;
using ConsoleApp1.Source.Graphics;
using ConsoleApp1.Source.Mesh;
using Graphics;
using Minecraft.Graphics;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using SixLabors.ImageSharp.Processing;
using PolygonMode = Silk.NET.OpenGL.PolygonMode;

namespace Minecraft
{
    public struct DirectionalLight
    {
        public Vector3 direction;
        public Vector3 ambient;
        public Vector3 diffuse;
        public Vector3 specular;
    }
    
    class Program
    {
        private static IWindow window;
        private static GL Gl;
        private static IKeyboard primaryKeyboard;
        
        private static Renderer renderer; // TODO move the rendering part to this class
        
        private static BufferObject<float> Vbo;
        // private static BufferObject<uint> Ebo;
        private static VertexArrayObject<float, uint> Vao;
        private static Texture2D _texture2D, Texture2;
        private static uint ComputeTexture;
        private static Shader Shader;
        
        private static ComputeShader computeShader;
        private static ComputeShader meshComputeShader;
        
        private static Material Material;
        private static Material Material2;
        
        //Setup the camera's location, directions, and movement speed
        private static FPSCamera fps_camera = new FPSCamera();
        private static OrbitCamera orbit_camera = new OrbitCamera();
        private static Camera camera = fps_camera;

        private static float Time;
        
        private static bool wireframeMode;
        
        private enum ViewMode
        {
            FirstPerson = 0,
            Orbit = 1
        }

        private static ViewMode viewMode = ViewMode.FirstPerson;
        
        private static float CameraZoom = 45f;
        private static float CameraRadius = 10f;
        
        // Cube
        // private static Cube cubeMesh = new Cube(Gl);
        //private static Mesh jsonMesh;
        //private static ObjLoader objMesh;

        private static DirectionalLight dirLight;

        private static int chunkSize = 1;
        private static ChunkMesh[,] chunks;
        
        private static Vector3 cubePosition = new Vector3(8, 8, 8);
        
        public static void Main(string[] args)
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1280, 720);
            options.Title = "OpenGL Application";
            var flags = ContextFlags.ForwardCompatible;
            
            flags |= ContextFlags.Debug;
            
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, flags, new APIVersion(4, 6));
            options.PreferredDepthBufferBits = 24;
            window = Window.Create(options);
            renderer = new Renderer(Gl, window);

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
            
            // Gl.Enable(EnableCap.CullFace);
            
            #if DEBUG
            SetupDebugCallback();
            #endif
            
            Shader = new Shader(Gl, "../../../Assets/Shaders/VoxelVertexShader.glsl", "../../../Assets/Shaders/VoxelFragmentShader.glsl");
            _texture2D = new Texture2D(Gl, "../../../Assets/Textures/cobblestone.png");

            computeShader = new ComputeShader(Gl, "../../../Assets/Shaders/VoxelComputeShader.glsl");

            dirLight = new DirectionalLight()
            {
                direction = new Vector3(-1 , -1, 0),
                ambient = new Vector3(0.8f),
                diffuse = new Vector3(0.8f),
                specular = new Vector3(0.2f)
            };
            
            chunks = new ChunkMesh[chunkSize,chunkSize];

            for (int x = 0; x < chunks.GetLength(0); x++)
            {
                for (int z = 0; z < chunks.GetLength(1); z++)
                {
                    chunks[x,z] = new ChunkMesh(Gl, Shader, new Vector2(x, z), new Vector2(chunks.GetLength(0), chunks.GetLength(1)));
                }
            }
        }

        private static void OnUpdate(double deltaTime)
        {
            var moveSpeed = 6f * (float) deltaTime;
            
            // camera.Rotate(0,0);

            if (primaryKeyboard.IsKeyPressed(Key.W))
            {
                //Move forwards
                camera.Position += camera.Front * moveSpeed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.S))
            {
                //Move backwards
                camera.Position -= camera.Front * moveSpeed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.A))
            {
                //Move left
                camera.Position -= Vector3.Normalize(Vector3.Cross(camera.Front, World.Up)) * moveSpeed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.D))
            {
                //Move right
                camera.Position += Vector3.Normalize(Vector3.Cross(camera.Front, World.Up)) * moveSpeed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.E))
            {
                Vector3 right = Vector3.Normalize(Vector3.Cross(camera.Front, World.Up));
                
                //Move upward
                camera.Position += Vector3.Normalize(Vector3.Cross(right, camera.Front)) * moveSpeed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.Q))
            {
                Vector3 right = Vector3.Normalize(Vector3.Cross(camera.Front, World.Up));
                
                //Move downward
                camera.Position -= Vector3.Normalize(Vector3.Cross(right, camera.Front)) * moveSpeed;
            }
            
            if (primaryKeyboard.IsKeyPressed(Key.Up))
            {
                // Zooms
                camera.SetFov(0.5f);
            }
            else if (primaryKeyboard.IsKeyPressed(Key.Down))
            {
                camera.SetFov(-0.5f);
            }
            
            //Console.WriteLine($"{camera.transform.Position} {camera.transform.Rotation}");
        }

        private static void OnRender(double deltaTime)
        {
            orbit_camera.SetLookAt(cubePosition);
            orbit_camera.Rotate(camera.CameraYaw, camera.CameraPitch);
            orbit_camera.SetRadius(CameraRadius);

            // dirLight.direction =  Vector3.Zero - camera.Position;

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
            //Gl.Enable(GLEnum.Blend);
            //Gl.BlendFunc(GLEnum.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Clear((uint) (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            BackgroundColor(190, 228, 230);
            
            // Use compute shader to refresh each frame here
            
            var difference = (float) (window.Time * 100);

            var size = window.FramebufferSize;
            
            var view = camera.GetViewMatrix();
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(camera.FieldOfView),
                (float) size.X / size.Y, 0.1f, 500.0f);

            Shader.Use();
            Shader.SetUniform("dirLight.direction", dirLight.direction);
            Shader.SetUniform("dirLight.ambient", dirLight.ambient);
            Shader.SetUniform("dirLight.diffuse", dirLight.diffuse);
            Shader.SetUniform("dirLight.specular", dirLight.specular);
            Shader.SetUniform("viewPos", camera.Position);
            
            for (int x = 0; x < chunks.GetLength(0); x++)
            {
                for (int z = 0; z < chunks.GetLength(1); z++)
                {
                    chunks[x,z].Render(view, projection, (float) window.Time);
                }
            }
        }

        private static void OnFramebufferResize(Vector2D<int> newSize)
        {
            Gl.Viewport(newSize);
        }

        private static void OnMouseMove(IMouse mouse, Vector2 position)
        {
            camera.OnMouseMove(mouse, position);
        }

        private static void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
        {
            switch (viewMode)
            {
                case ViewMode.FirstPerson:
                    //We don't want to be able to zoom in too close or too far away so clamp to these values
                    camera.SetFov(scrollWheel.Y);
                    break;
                case ViewMode.Orbit:
                    CameraRadius -= scrollWheel.Y * 2;
                    CameraRadius = Math.Clamp(CameraRadius, 0.5f, 99);
                    //Console.WriteLine(CameraRadius);
                    break;
            }
        }

        private static void OnClose()
        {
            Dispose();
        }
        
        private static void Dispose()
        {
            for (int x = 0; x < chunks.GetLength(0); x++)
            {
                for (int z = 0; z < chunks.GetLength(1); z++)
                {
                    chunks[x,z].Dispose();
                }
            }
            Shader.Dispose();
            _texture2D.Dispose();
        }

        private static void KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            // Close window
            if (key == Key.Escape)
            {
                window.Close();
            }
            
            // Wireframe Mode
            if (key == Key.Z)
            {
                wireframeMode = !wireframeMode;
                if (wireframeMode)
                {
                    Gl.PolygonMode((GLEnum) GLEnum.FrontAndBack, (PolygonMode) PolygonMode.Line);
                }
                else
                {
                    Gl.PolygonMode(GLEnum.FrontAndBack, PolygonMode.Fill);
                }
            }
            
            // Camera Switch

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
                
                Console.WriteLine($"Switching view mode to {viewMode}");
            }

            if (key == Key.F11)
            {
                if (window.WindowState == WindowState.Fullscreen)
                {
                    window.WindowState = WindowState.Normal; // Bascule en mode fenêtré
                }
                else
                {
                    window.WindowState = WindowState.Fullscreen; // Bascule en mode plein écran
                }
            }
        }

        private static void BackgroundColor(uint r, uint g, uint b)
        {
            Gl.ClearColor((float) r / 255, (float) g / 255, (float) b / 255, 1);
        }
        
#if DEBUG
        private static void SetupDebugCallback()
        {
            unsafe
            {
                DebugProc callback = (source, type, id, severity, length, message, userParam) =>
                {
                    var messageStr = Marshal.PtrToStringAnsi(message, length);
                    Console.WriteLine($"{source}:{type}[{severity}]({id}) {messageStr}");
                };
                Gl.DebugMessageCallback(callback, null);
                Gl.DebugMessageControl(DebugSource.DontCare, DebugType.DontCare, DebugSeverity.DontCare, 0, null, true);
            }
        }
#endif
    }
}
