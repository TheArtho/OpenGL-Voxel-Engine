using System.Numerics;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Minecraft;

public class ChunkMesh : IDisposable
{
    private GL _gl;

    private uint vao, ssbo, commandBuffer;

    private uint chunkTexture3D;
    private ComputeShader computeChunkShader, computeMeshShader;
    
    private Shader shader;
    private Texture2D _texture2D;
    
    private uint vbo;

    private uint blockCount;

    public Vector2 coordinates { get; private set; }
    public Vector2 worldSize { get; private set; }
    
    // Struct pour les commandes de dessin
    struct DrawArraysIndirectCommand
    {
        public uint Count;
        public uint InstanceCount;
        public uint First;
        public uint BaseInstance;
    }

    public ChunkMesh(GL _gl, Shader shader, Vector2 coordinates, Vector2 worldSize)
    {
        this._gl = _gl;
        this.shader = shader;
        this.coordinates = coordinates;
        this.worldSize = worldSize;

        TextureAtlasGenerator atlasGenerator = new TextureAtlasGenerator();
        Image<Rgba32> paddedTextureAtlas = atlasGenerator.GeneratePaddedAtlas(Image.Load<Rgba32>("../../../Assets/Textures/texture_atlas.png"));
        
        computeChunkShader = new ComputeShader(_gl, "../../../Assets/Shaders/VoxelComputeShader.glsl");
        computeMeshShader = new ComputeShader(_gl, "../../../Assets/Shaders/ComputeMesh.glsl");
        _texture2D = new Texture2D(_gl, paddedTextureAtlas);
        
        InitializeBuffer();
        UpdateChunk();
        GenerateMesh();
    }

    private unsafe void UpdateChunk()
    {
        CreateTexture();
        GenerateChunk(0.0f);
    }
    
    private unsafe void InitializeBuffer()
    {
        uint vertexDataSize = 4 * 4 * 2;
        
        Console.WriteLine($"vertexData size {vertexDataSize}, uint size {sizeof(uint)}");
        
        // Setup VAO
        vao = _gl.GenVertexArray();
        _gl.BindVertexArray(vao);
    
        // Create and bind SSBO for vertices
        ssbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, ssbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (vertexDataSize * 64 * 64 * 64 * 36 / 2), null, BufferUsageARB.DynamicDraw);
        
        // Create buffer for draw commands
        commandBuffer = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, commandBuffer);
        _gl.BufferData(BufferTargetARB.DrawIndirectBuffer, (nuint)(sizeof(DrawArraysIndirectCommand)), null, BufferUsageARB.DynamicDraw);
        
        // position + voxelValue (vec)
        _gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexDataSize, 0);
        _gl.EnableVertexAttribArray(0);
        // uv (vec)
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexDataSize, sizeof(float) * 4);
        _gl.EnableVertexAttribArray(1);
        // faceID (uint)
        //_gl.VertexAttribPointer(2, 1, VertexAttribPointerType.UnsignedInt, false, vertexDataSize, sizeof(float) * 4 * 2);
        //_gl.EnableVertexAttribArray(2);
    
        _gl.BindVertexArray(0); // Unbind VAO to prevent accidental modification
    }

    private unsafe void CreateTexture()
    {
        // Compute Texture
        chunkTexture3D = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture3D, chunkTexture3D);
            
        _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        _gl.TexImage3D(TextureTarget.Texture3D, 0, InternalFormat.R8ui, 64, 64, 64, 0, PixelFormat.RedInteger, PixelType.UnsignedInt, null);
        _gl.BindTexture(TextureTarget.Texture3D, 0);
    }

    private void GenerateChunk(float time)
    {
        // Chunk Generation shader and store it to the chunkTexture3D
        computeChunkShader.Use();
            
        computeChunkShader.SetUniform("time", time);
        computeChunkShader.SetUniform("offset", new Vector3(coordinates.X*16, 0, coordinates.Y*16));
        
        // Binding of the chunkData texture3D to 0
        _gl.BindTexture(TextureTarget.Texture3D, chunkTexture3D);
        _gl.BindImageTexture (0, chunkTexture3D, 0, false, 0, GLEnum.WriteOnly, InternalFormat.R8ui);
        
        // Calculation of the chunk data through a compute shader
        _gl.DispatchCompute(16,16, 16);
        _gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);
        
        // Unbinding of the texture3D
        _gl.BindImageTexture (0, 0, 0, false, 0, GLEnum.WriteOnly, InternalFormat.R8ui);
        _gl.BindTexture(TextureTarget.Texture3D, 0);
            
        computeChunkShader.Unbind();
    }
    
    private unsafe void GenerateMesh()
    {
        DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
        {
            Count = 0,  // Ensure that this count is properly updated in your compute shader or elsewhere necessary
            InstanceCount = 1,
            First = 0,
            BaseInstance = 0
        };
    
        _gl.NamedBufferSubData(commandBuffer, 0, (nuint)sizeof(DrawArraysIndirectCommand), in cmd);
        
        // Use the compute shader designed to populate the mesh data
        computeMeshShader.Use();

        // Bind the texture that contains chunk data which will be read by the compute shader
        _gl.BindTexture(TextureTarget.Texture3D, chunkTexture3D);
        _gl.BindImageTexture(0, chunkTexture3D, 0, false, 0, GLEnum.ReadOnly, InternalFormat.R8ui);

        // Ensure the SSBO is bound to the appropriate binding point that the compute shader expects
        _gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 1, ssbo);

        // Also ensure the command buffer is correctly bound to the shader
        _gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 2, commandBuffer);

        // Dispatch compute 4x4x4
        _gl.DispatchCompute(4, 4, 4); // Adjust the dispatch parameters based on your data needs
        
        // Wait for all compute operations to complete before using the data in subsequent rendering calls
        _gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);

        // Unbind the texture as it is no longer needed
        _gl.BindImageTexture(0, 0, 0, false, 0, GLEnum.ReadOnly, InternalFormat.R8ui);
        _gl.BindTexture(TextureTarget.Texture3D, 0);
    }


    public unsafe void Render(Matrix4x4 view, Matrix4x4 projection, float time)
    {
        // GenerateChunk(time);
        // GenerateMesh();
        
        // shader.Use();
        
        _texture2D.Bind(0);
        
        shader.SetUniform("uView", view);
        shader.SetUniform("uProjection", projection);
        
        var model = Matrix4x4.CreateTranslation((Get3DCoord() - new Vector3((worldSize.X-1) / 2, 0, (worldSize.Y-1) / 2)) * 16);
        shader.SetUniform("uModel", model);
        
        _gl.BindVertexArray(vao);
        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, commandBuffer);
        _gl.DrawArraysIndirect(PrimitiveType.Triangles, null);
    }

    public void Dispose()
    {
        _gl.DeleteBuffer(vbo);
        //_vbo.Dispose();
        //_vao.Dispose();
        shader.Dispose();
    }

    public void glGetError()
    {
        GLEnum error = _gl.GetError();
        if (error != GLEnum.NoError)
        {
            Console.WriteLine($"OpenGL Error: {error}");
        }
    }

    public Vector3 Get3DCoord()
    {
        return new Vector3(coordinates.X, 0, coordinates.Y);
    }
}