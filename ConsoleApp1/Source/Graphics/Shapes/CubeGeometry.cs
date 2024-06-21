using Minecraft;
using Silk.NET.OpenGL;

public class CubeGeometry
{
    private GL _gl;
    private VertexArrayObject<float, uint> _vao;
    private BufferObject<float> _vbo;
    
    public CubeGeometry(GL gl)
    {
        _gl = gl;

        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        float[] vertices =
        [
            //X    Y      Z      U     V
            -0.5f, -0.5f, -0.5f, 0.0f, 1.0f,
            0.5f, -0.5f, -0.5f, 1.0f, 1.0f,
            0.5f, 0.5f, -0.5f, 1.0f, 0.0f,
            0.5f, 0.5f, -0.5f, 1.0f, 0.0f,
            -0.5f, 0.5f, -0.5f, 0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f, 0.0f, 1.0f,

            -0.5f, -0.5f, 0.5f, 0.0f, 1.0f,
            0.5f, -0.5f, 0.5f, 1.0f, 1.0f,
            0.5f, 0.5f, 0.5f, 1.0f, 0.0f,
            0.5f, 0.5f, 0.5f, 1.0f, 0.0f,
            -0.5f, 0.5f, 0.5f, 0.0f, 0.0f,
            -0.5f, -0.5f, 0.5f, 0.0f, 1.0f,

            -0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
            -0.5f, 0.5f, -0.5f, 1.0f, 0.0f,
            -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
            -0.5f, -0.5f, 0.5f, 0.0f, 1.0f,
            -0.5f, 0.5f, 0.5f, 1.0f, 1.0f,

            0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
            0.5f, 0.5f, -0.5f, 1.0f, 0.0f,
            0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
            0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
            0.5f, -0.5f, 0.5f, 0.0f, 1.0f,
            0.5f, 0.5f, 0.5f, 1.0f, 1.0f,

            -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,
            0.5f, -0.5f, -0.5f, 1.0f, 0.0f,
            0.5f, -0.5f, 0.5f, 1.0f, 1.0f,
            0.5f, -0.5f, 0.5f, 1.0f, 1.0f,
            -0.5f, -0.5f, 0.5f, 0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f, 0.0f, 0.0f,

            -0.5f, 0.5f, -0.5f, 0.0f, 0.0f,
            0.5f, 0.5f, -0.5f, 1.0f, 0.0f,
            0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
            0.5f, 0.5f, 0.5f, 1.0f, 1.0f,
            -0.5f, 0.5f, 0.5f, 0.0f, 1.0f,
            -0.5f, 0.5f, -0.5f, 0.0f, 0.0f
        ];
        
        _vbo = new BufferObject<float>(_gl, new Span<float>(vertices), BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
    }

    public void Draw()
    {
        _vao.Bind();
        _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }
}