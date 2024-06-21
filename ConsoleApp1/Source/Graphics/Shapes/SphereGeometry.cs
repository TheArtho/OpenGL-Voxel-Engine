using ConsoleApp1.Source.Graphics.Shapes;
using Minecraft;
using Silk.NET.OpenGL;

public class SphereGeometry : Geometry
{
    private GL _gl;
    private VertexArrayObject<float, uint> _vao;
    private BufferObject<float> _vbo;

    private float[] vertices;
    
    public SphereGeometry(GL gl)
    {
        _gl = gl;

        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        this.vertices = GenerateVertices();
        
        _vbo = new BufferObject<float>(_gl, new Span<float>(vertices), BufferTargetARB.ArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo);
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
    }

    private float[] GenerateVertices(float radius = 1)
{
    List<float> vertices = new List<float>();
    List<int> indices = new List<int>();
    
    int sectorCount = 36;
    int stackCount = 18;
    
    float x, y, z, xy;                              // vertex position
    float nx, ny, nz, lengthInv = 1.0f / radius;    // vertex normal
    float s, t;                                     // vertex texCoord

    float sectorStep = 2 * (float) Math.PI / sectorCount;
    float stackStep = (float) Math.PI / stackCount;
    float sectorAngle, stackAngle;

    for(int i = 0; i <= stackCount; ++i)
    {
        stackAngle = (float) Math.PI / 2 - i * stackStep;  // starting from pi/2 to -pi/2
        xy = radius * (float)Math.Cos(stackAngle);         // r * cos(u)
        z = radius * (float)Math.Sin(stackAngle);          // r * sin(u)

        for(int j = 0; j <= sectorCount; ++j)
        {
            sectorAngle = j * sectorStep;                  // starting from 0 to 2pi

            // vertex position (x, y, z)
            x = xy * (float)Math.Cos(sectorAngle);         // r * cos(u) * cos(v)
            y = xy * (float)Math.Sin(sectorAngle);         // r * cos(u) * sin(v)
            vertices.Add(x);
            vertices.Add(y);
            vertices.Add(z);

            // vertex tex coord (s, t) range between [0, 1]
            s = (float)j / sectorCount;
            t = (float)i / stackCount;
            vertices.Add(s);
            vertices.Add(t);
        }
    }

    // Generating indices for triangles
    int k1, k2;
    for (int i = 0; i < stackCount; ++i)
    {
        k1 = i * (sectorCount + 1);     // beginning of current stack
        k2 = k1 + sectorCount + 1;      // beginning of next stack

        for (int j = 0; j < sectorCount; ++j, ++k1, ++k2)
        {
            // 2 triangles per sector excluding first and last stacks
            // k1 => k2 => k1+1
            if (i != 0)
            {
                indices.Add(k1);
                indices.Add(k2);
                indices.Add(k1 + 1);
            }

            // k1+1 => k2 => k2+1
            if (i != (stackCount - 1))
            {
                indices.Add(k1 + 1);
                indices.Add(k2);
                indices.Add(k2 + 1);
            }
        }
    }

    // Optionally convert indices to float and add to vertices, or handle separately
    // This example just returns the vertices as is
    return vertices.ToArray();
}


    public override void Draw()
    {
        _vao.Bind();
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint) vertices.Length * 5);
    }
}