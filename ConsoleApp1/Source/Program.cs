using System.Net.Mime;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Minecraft.Core;
using Minecraft.Game;
using Minecraft.Graphics;
using Minecraft.Utils;
using Newtonsoft.Json;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using SixLabors.ImageSharp.Processing;
using StbImageSharp;
using BlockData = Minecraft.JsonData.BlockData;
using PolygonMode = Silk.NET.OpenGL.PolygonMode;
using World = Graphics.World;

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
        public static IWindow window;
        private static GL Gl;
        private static IKeyboard primaryKeyboard;
        private static ImGuiController imguiController;
        
        private static Renderer renderer;
        
        private static uint ComputeTexture;
        private static Shader unlitColorShader;
        
        private static ComputeShader meshComputeShader;
        
        private static Material Material;
        private static Material Material2;
        
        //Setup the camera's location, directions, and movement speed
        private static FPSCamera fps_camera = new FPSCamera();
        private static OrbitCamera orbit_camera = new OrbitCamera();

        private static float Time;
        
        private static bool wireframeMode;

        private static Game.World world;
        
        private enum ViewMode
        {
            FirstPerson = 0,
            Orbit = 1
        }

        private static ViewMode viewMode = ViewMode.FirstPerson;
        
        private static float CameraZoom = 45f;
        private static float CameraRadius = 80f;
        
        // Cube
        // private static Cube cubeMesh = new Cube(Gl);
        //private static Mesh jsonMesh;
        //private static ObjLoader objMesh;

        private static DirectionalLight dirLight;

        private static int chunkSize = 6;
        private static ChunkMesh[,] chunks;
        private static CubeGeometry cube;
        
        private static Vector3 cubePosition = new Vector3(8, 8, 8);
        
        private static List<Vector3> lightPositions;
        private static List<Vector3> lightColors;

        private static float lightQuadratic;
        private static float lightLinear;
        private static float lightOffset;

        private static float moveSpeed = 6;
        
        
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

            Engine.MainCamera = fps_camera;
            
            imguiController = new ImGuiController(Gl, window, input);
            
            /*
            #if DEBUG
            SetupDebugCallback();
            #endif
            */
            
            // Instantiate the block database
            List<Minecraft.JsonData.Block> blocks = JsonConvert.DeserializeObject<List<Minecraft.JsonData.Block>>(File.ReadAllText(AssetPaths.blockListPath));
            foreach (var block in blocks)
            {
                BlockData blockData = JsonConvert.DeserializeObject<BlockData>(File.ReadAllText($"{AssetPaths.blockData}/{block.Reference}.json"));

                Game.BlockData b = new Game.BlockData((uint) block.BlockID, blockData);
            }

            Game.BlockData.SetFaceBlockDataArray();
            
            cube = new CubeGeometry(Gl);
            
            world = new Game.World(Gl, 0);

            renderer = new Renderer(Gl, window, Engine.MainCamera);
            renderer.world = world;

            Engine.MainCamera.Position = new Vector3(8, Chunk.kDefaultChunkHeight, 20);
        }

        private static void OnUpdate(double deltaTime)
        {
            Vector3 cameraPosition = Engine.MainCamera.Position;
            
            var speed = moveSpeed * (float) deltaTime;

            if (primaryKeyboard.IsKeyPressed(Key.W))
            {
                //Move forwards
                Engine.MainCamera.Position += Engine.MainCamera.Front * speed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.S))
            {
                //Move backwards
                Engine.MainCamera.Position -= Engine.MainCamera.Front * speed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.A))
            {
                //Move left
                Engine.MainCamera.Position -= Vector3.Normalize(Vector3.Cross(Engine.MainCamera.Front, World.Up)) * speed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.D))
            {
                //Move right
                Engine.MainCamera.Position += Vector3.Normalize(Vector3.Cross(Engine.MainCamera.Front, World.Up)) * speed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.E))
            {
                Vector3 right = Vector3.Normalize(Vector3.Cross(Engine.MainCamera.Front, World.Up));
                
                //Move upward
                Engine.MainCamera.Position += Vector3.Normalize(Vector3.Cross(right, Engine.MainCamera.Front)) * speed;
            }
            if (primaryKeyboard.IsKeyPressed(Key.Q))
            {
                Vector3 right = Vector3.Normalize(Vector3.Cross(Engine.MainCamera.Front, World.Up));
                
                //Move downward
                Engine.MainCamera.Position -= Vector3.Normalize(Vector3.Cross(right, Engine.MainCamera.Front)) * speed;
            }
            
            if (primaryKeyboard.IsKeyPressed(Key.Up))
            {
                // Zooms
                Engine.MainCamera.SetFov(0.5f);
            }
            else if (primaryKeyboard.IsKeyPressed(Key.Down))
            {
                Engine.MainCamera.SetFov(-0.5f);
            }
            
            imguiController.Update((float)deltaTime);

            if (!Game.World.PositionsEquals(cameraPosition, Engine.MainCamera.Position))
            {
                world.needChunkUpdate = true;
            }
        }

        private static void OnRender(double deltaTime)
        {
            if (world.needChunkUpdate)
            {
                world.UpdateLODs();
                world.needChunkUpdate = false;
            }
            
            orbit_camera.SetLookAt(cubePosition + global::Graphics.World.Up * ((float) Chunk.kDefaultChunkHeight / 2));
            orbit_camera.Rotate(Engine.MainCamera.CameraYaw, Engine.MainCamera.CameraPitch);
            orbit_camera.SetRadius(CameraRadius);
            
            renderer.OnRender();
        }

        private static void ImGuiRender()
        {
            imguiController.Render();
        
            ImGui.Begin("Settings");
            
            if (ImGui.SliderFloat("Quadratic", ref lightQuadratic, 0.0f, 5.0f))
            {
                 
            }

            if (ImGui.SliderFloat("Linear", ref lightLinear, 0.0f, 5.0f))
            {
               
            }

            if (ImGui.SliderFloat("Offset Y", ref lightOffset, -10, 10))
            {
                
            }

            ImGui.End();
        }

        private static void OnFramebufferResize(Vector2D<int> newSize)
        {
            renderer.RefreshFramebuffer(newSize);
            
            Gl.Viewport(0, 0, (uint) newSize.X, (uint) newSize.Y);
        }

        private static void OnMouseMove(IMouse mouse, Vector2 position)
        {
            Engine.MainCamera.OnMouseMove(mouse, position);
        }

        private static void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
        {
            switch (viewMode)
            {
                case ViewMode.FirstPerson:
                    //We don't want to be able to zoom in too close or too far away so clamp to these values
                    moveSpeed = float.Clamp(moveSpeed + scrollWheel.Y, 1, 100);
                    // Engine.MainCamera.SetFov(scrollWheel.Y);
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
            renderer.Dispose();
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
                    Engine.MainCamera = orbit_camera;
                }
                else if (viewMode == ViewMode.Orbit)
                {
                    viewMode = ViewMode.FirstPerson;
                    Engine.MainCamera = fps_camera;
                }
                renderer.camera = Engine.MainCamera;
                
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

            if (key == Key.Space)
            {
                renderer.SwapLightParams();
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
