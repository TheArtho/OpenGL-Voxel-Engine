using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.Linq;
using System.Numerics;
using ConsoleApp1.Source;
using ConsoleApp1.Source.Graphics;
using ConsoleApp1.Source.Mesh;
using Graphics;
using Minecraft.Graphics;
using Silk.NET.Maths;

namespace Minecraft
{
    class Program
    {
        private static IWindow window;
        private static GL Gl;
        private static IKeyboard primaryKeyboard;
        
        private static Renderer renderer; // TODO move the rendering part to this class
        
        private static BufferObject<float> Vbo;
        // private static BufferObject<uint> Ebo;
        private static VertexArrayObject<float, uint> Vao;
        private static Texture Texture, Texture2;
        private static Shader Shader;
        private static Material Material;
        private static Material Material2;
        
        //Setup the camera's location, directions, and movement speed
        private static FPSCamera fps_camera = new FPSCamera();
        private static OrbitCamera orbit_camera = new OrbitCamera();
        private static Camera camera = fps_camera;
        
        private static bool wireframeMode = false;
        
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
        private static Mesh jsonMesh;
        private static ObjLoader objMesh;
        private static Vector3 cubePosition = new Vector3(0, 0, 0);
        
        private static readonly uint[] Indices =
        {
            0, 1, 3,
            1, 2, 3
        };
        

        private static void Main(string[] args)
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = "OpenGL Application";
            window = Window.Create(options);
            renderer = new Renderer(Gl, window);

            window.Load += OnLoad;
            window.Update += OnUpdate;
            window.Render += OnRender;
            //window.Render += renderer.OnRender;
            window.FramebufferResize += OnFramebufferResize;
            window.Closing += OnClose;
            

            window.Run();

            window.Dispose();
        }

        private static void OnLoad()
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
            
            //Gl.Enable(EnableCap.CullFace);
            
            /*

            //Ebo = new BufferObject<uint>(Gl, Indices, BufferTargetARB.ElementArrayBuffer);
            Vbo = new BufferObject<float>(Gl, cubeMesh.Vertices, BufferTargetARB.ArrayBuffer);
            Vao = new VertexArrayObject<float, uint>(Gl, Vbo);

            Vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
            Vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
            
            */
            
            Shader = new Shader(Gl, "../../../Assets/Shaders/shader.vert", "../../../Assets/Shaders/shader.frag");

            Texture = new Texture(Gl, "../../../Assets/Textures/zoroark.png");
            Texture2 = new Texture(Gl, "../../../Assets/Textures/bulbasaur.png");
            
            Material = new Material(Shader, Texture);
            Material2 = new Material(Shader, Texture2);

            jsonMesh = new Mesh(Gl, JsonMeshLoader.LoadGeometryFile("../../../Assets/Geometries/bulbasaur.geo.json"), Material2);

            objMesh = new ObjLoader(Gl, "../../../Assets/Geometries/zoroark.obj", Material);
        }

        private static unsafe void OnUpdate(double deltaTime)
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

        private static unsafe void OnRender(double deltaTime)
        {
            orbit_camera.SetLookAt(cubePosition);
            orbit_camera.Rotate(camera.CameraYaw, camera.CameraPitch);
            orbit_camera.SetRadius(CameraRadius);

            Gl.Enable(EnableCap.DepthTest);
            //Gl.Enable(GLEnum.Blend);
            //Gl.BlendFunc(GLEnum.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Gl.Clear((uint) (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

            // Vao.Bind();
            // Texture.Bind(0);
            Shader.Use();

            Shader.SetUniform("time", (float) window.Time);
            // Shader.SetUniform("uTexture0", 0);
            // Shader.SetUniform("uTexture1", 1);

            //Use elapsed time to convert to radians to allow our cube to rotate over time
            var difference = (float) (window.Time * 0);

            var size = window.FramebufferSize;
            
            var view = camera.GetViewMatrix();
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(camera.FieldOfView),
                (float) size.X / size.Y, 0.1f, 500.0f);
            
            Shader.SetUniform("uView", view);
            Shader.SetUniform("uProjection", projection);
            
            var model = Matrix4x4.CreateTranslation(cubePosition); // * Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference)) * Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference));
            Shader.SetUniform("uModel", model);
            
            // jsonMesh.Render(model, camera);
            
            model = Matrix4x4.CreateScale(2);
            Shader.SetUniform("uModel", model);
            
            objMesh.Render();

            /*
            int[,] chunk = new int[,]
            {
                { 1, 1, 1, 0, 1, 1, 0, 0, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 0 },
                { 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0 },
                { 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 1, 0 },
                { 1, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0 },
                { 1, 1, 1, 0, 0, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0, 0, 1, 0, 1, 0 }
            };

            for (int y = 0; y < chunk.GetLength(0); y++)
            {
                for (int x = 0; x < chunk.GetLength(1); x++)
                {
                    if (chunk[y,x] == 1)
                    {
                        var model = Matrix4x4.CreateTranslation(cubePosition + new Vector3(x - chunk.GetLength(1) / 2, -y, 0)); // * Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference)) * Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference));
                    
                        Shader.SetUniform("uModel", model);
            
                        //We're drawing with just vertices and no indices, and it takes 36 vertices to have a six-sided textured cube
                        Gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
                        
                        // Console.WriteLine($"block créé à {cubePosition + new Vector3(x, y, -5)}");
                    }
                }
            }
            */
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
            // Vbo.Dispose();
            // Vao.Dispose();
            jsonMesh.Dispose();
            objMesh.Dispose();
            Shader.Dispose();
            Texture.Dispose();
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
                    Gl.PolygonMode(GLEnum.FrontAndBack, PolygonMode.Line);
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
    }
}
