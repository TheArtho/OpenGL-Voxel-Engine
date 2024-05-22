using Silk.NET.OpenGL;

namespace Minecraft;

public class ChunkMesh
{
    private GL _gl;
    private Shader shader;
    
    private VertexArrayObject<float, uint> _vao;
    private BufferObject<float> _vbo;
    
    float[] cube = {
        // Front face
        -1.0f,  1.0f,  1.0f,  0.0f, 1.0f, 1.0f,
        -1.0f, -1.0f,  1.0f,  0.0f, 0.0f, 1.0f,
        1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
        1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
        1.0f,  1.0f,  1.0f,  1.0f, 1.0f, 1.0f,
        -1.0f,  1.0f,  1.0f,  0.0f, 1.0f, 1.0f,

        // Back face
        -1.0f,  1.0f, -1.0f,  0.0f, 1.0f, 0.0f,
        -1.0f, -1.0f, -1.0f,  0.0f, 0.0f, 0.0f,
        1.0f, -1.0f, -1.0f,  1.0f, 0.0f, 0.0f,
        1.0f, -1.0f, -1.0f,  1.0f, 0.0f, 0.0f,
        1.0f,  1.0f, -1.0f,  1.0f, 1.0f, 0.0f,
        -1.0f,  1.0f, -1.0f,  0.0f, 1.0f, 0.0f,

        // Left face
        -1.0f,  1.0f, -1.0f,  0.0f, 1.0f, 0.0f,
        -1.0f, -1.0f, -1.0f,  0.0f, 0.0f, 0.0f,
        -1.0f, -1.0f,  1.0f,  0.0f, 0.0f, 1.0f,
        -1.0f, -1.0f,  1.0f,  0.0f, 0.0f, 1.0f,
        -1.0f,  1.0f,  1.0f,  0.0f, 1.0f, 1.0f,
        -1.0f,  1.0f, -1.0f,  0.0f, 1.0f, 0.0f,

        // Right face
        1.0f,  1.0f, -1.0f,  1.0f, 1.0f, 0.0f,
        1.0f, -1.0f, -1.0f,  1.0f, 0.0f, 0.0f,
        1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
        1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
        1.0f,  1.0f,  1.0f,  1.0f, 1.0f, 1.0f,
        1.0f,  1.0f, -1.0f,  1.0f, 1.0f, 0.0f,

        // Top face
        -1.0f,  1.0f, -1.0f,  0.0f, 1.0f, 0.0f,
        -1.0f,  1.0f,  1.0f,  0.0f, 1.0f, 1.0f,
        1.0f,  1.0f,  1.0f,  1.0f, 1.0f, 1.0f,
        1.0f,  1.0f,  1.0f,  1.0f, 1.0f, 1.0f,
        1.0f,  1.0f, -1.0f,  1.0f, 1.0f, 0.0f,
        -1.0f,  1.0f, -1.0f,  0.0f, 1.0f, 0.0f,

        // Bottom face
        -1.0f, -1.0f, -1.0f,  0.0f, 0.0f, 0.0f,
        -1.0f, -1.0f,  1.0f,  0.0f, 0.0f, 1.0f,
        1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
        1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
        1.0f, -1.0f, -1.0f,  1.0f, 0.0f, 0.0f,
        -1.0f, -1.0f, -1.0f,  0.0f, 0.0f, 0.0f
    };


    public ChunkMesh(GL _gl, Shader shader)
    {
        this._gl = _gl;
        this.shader = shader;
        
        InitializeBuffer();
    }

    private void InitializeBuffer()
    {
        _vbo = new BufferObject<float>(_gl, cube, BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo);
        
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 6, 0);
        _vao.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 6, 3);
    }

    public void UseShader()
    {
        shader.Use();
    }

    public void Render(uint texture3D)
    {
        _vao.Bind();
        
        /*
        shader.SetUniform("voxelTexture", 0);
        
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture3D, texture3D);
        */
        
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint) cube.Length / 5);
    }

    public void Dispose()
    {
        _vbo.Dispose();
        _vao.Dispose();
        shader.Dispose();
    }
}