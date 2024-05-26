using System.Numerics;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Minecraft.Graphics;

public class Skybox
{
    /*
    private GL _gl;

    private Shader shader;
    
    List<Image<Rgba32>> images = new List<Image<Rgba32>>();

    private uint cubeMapTextureID;
    
    public unsafe Skybox(GL _gl, string path)
    {
        this._gl = _gl;
        
        GLEnum[] cube_map_target = new GLEnum[] {           
            GLEnum.TextureCubeMapNegativeX,
            GLEnum.TextureCubeMapPositiveX,
            GLEnum.TextureCubeMapNegativeY,
            GLEnum.TextureCubeMapPositiveY,
            GLEnum.TextureCubeMapNegativeZ,
            GLEnum.TextureCubeMapPositiveZ
        };
        
        images.Add(Image.Load<Rgba32>($"{path}_bk.jpg"));
        images.Add(Image.Load<Rgba32>($"{path}_bk.jpg"));
        images.Add(Image.Load<Rgba32>($"{path}_bk.jpg"));
        images.Add(Image.Load<Rgba32>($"{path}_bk.jpg"));
        images.Add(Image.Load<Rgba32>($"{path}_bk.jpg"));
        images.Add(Image.Load<Rgba32>($"{path}_bk.jpg"));

        cubeMapTextureID = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.TextureCubeMap, cubeMapTextureID);

        int i = 0;
        foreach (var image in images)
        {
            _gl.TexImage2D(cube_map_target[i], 0, InternalFormat.Rgba8, (uint) image.Width, (uint) image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            i++;
        }
    }

    public void Render(Matrix4x4 Model, Matrix4x4 View, Matrix4x4 Projection)
    {
        // Shader program binding (assuming you have loaded this elsewhere)
        shader.Use();

        // Set uniforms for matrices, removing translation component from the view matrix for a skybox
        Matrix4x4 viewMatrix = View;
        viewMatrix.Translation = Vector3.Zero;
        shader.SetUniform("uView", viewMatrix);
        shader.SetUniform("uProjection", Projection);

        // Bind the vertex array and texture
        _gl.BindVertexArray(vaoId);
        _gl.BindTexture(TextureTarget.TextureCubeMap, cubeMapTextureID);

        // Disable depth mask to draw skybox in the background
        _gl.DepthMask(false);
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);  // Assuming your cube has 36 vertices (6 faces * 2 triangles * 3 vertices)
        _gl.DepthMask(true);

        // Unbind VAO and texture
        _gl.BindVertexArray(0);
        _gl.BindTexture(TextureTarget.TextureCubeMap, 0);

        // Error handling and cleanup if necessary
    }
    */
}