using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Minecraft.Graphics;

public class Renderer
{
    private IWindow window;
    private GL Gl;
    
    private BufferObject<float> Vbo;
    // private static BufferObject<uint> Ebo;
    private VertexArrayObject<float, uint> Vao;
    private Texture Texture, Texture2;
    private Shader Shader;

    public Renderer(GL gl, IWindow window)
    {
        this.Gl = Gl;
        // window.Render += OnRender;
    }
    
    /*
    public unsafe void OnRender(double deltaTime)
    {
        orbit_camera.SetLookAt(cubePosition);
        orbit_camera.Rotate(CameraYaw, CameraPitch);
        orbit_camera.SetRadius(CameraRadius);

        Gl.Enable(EnableCap.DepthTest);
        Gl.Clear((uint) (ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        Vao.Bind();
        // Texture.Bind(0);
        Shader.Use();

        Shader.SetUniform("time", (float) window.Time);
        // Shader.SetUniform("uTexture0", 0);
        // Shader.SetUniform("uTexture1", 1);

        //Use elapsed time to convert to radians to allow our cube to rotate over time
        var difference = (float) (window.Time * 0);

        var size = window.FramebufferSize;

        var
            model = Matrix4x4
                .CreateTranslation(
                    cubePosition); // * Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference)) * Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference));
        var view = camera.GetViewMatrix();
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(camera.FieldOfView),
            (float) size.X / size.Y, 0.1f, 100.0f);

        Shader.SetUniform("uModel", model);
        Shader.SetUniform("uView", view);
        Shader.SetUniform("uProjection", projection);

        //We're drawing with just vertices and no indices, and it takes 36 vertices to have a six-sided textured cube
        Gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }
    */

    public void Dispose()
    {
        Vbo.Dispose();
        // Ebo.Dispose();
        Vao.Dispose();
        Shader.Dispose();
        Texture.Dispose();
    }
}