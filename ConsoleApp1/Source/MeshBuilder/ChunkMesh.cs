using System.Numerics;
using System.Runtime.InteropServices;
using Minecraft.Game;
using Minecraft.Utils;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Minecraft;

public class ChunkMesh : IDisposable
{
    private const bool StoreMeshOnCPU = true;
    
    private bool _disposed = false;
    
    private GL _gl;
    
    private uint opaque_vao, transparent_vao, opaque_ssbo, transparent_ssbo, opaque_commandBuffer, transparent_commandBuffer;

    private uint chunkTextureBuffer;
    private ComputeShader computeChunkShader, computeMeshShader;
    
    private Shader shader;
    private Texture2D _texture2D;

    private uint blockCount;

    public Vector2 coordinates { get; private set; }
    public Vector2 worldSize { get; private set; }

    public Vector3D<uint> groups = new Vector3D<uint>(4, 4, 4);

    public uint lodLevel = 0;
    
    // Struct pour les commandes de dessin
    struct DrawArraysIndirectCommand
    {
        public uint Count;
        public uint InstanceCount;
        public uint First;
        public uint BaseInstance;
    }

    public ChunkMesh(GL gl, Vector2 coordinates, ComputeShader computeMeshShader, Vector3D<uint> groups)
    {
        this._gl = gl;
        this.coordinates = coordinates;
        this.worldSize = worldSize;
        this.computeMeshShader = computeMeshShader;
        this.groups = groups;
    }

    public ChunkMesh(GL _gl, Shader shader, Vector2 coordinates, Vector2 worldSize)
    {
        this._gl = _gl;
        this.shader = shader;
        this.coordinates = coordinates;
        this.worldSize = worldSize;

        TextureAtlasGenerator atlasGenerator = new TextureAtlasGenerator();
        Image<Rgba32> paddedTextureAtlas = atlasGenerator.GeneratePaddedAtlas(Image.Load<Rgba32>(AssetPaths.texturePath + "texture_atlas.png"));
        
        computeChunkShader = new ComputeShader(_gl, AssetPaths.shaderPath + "ComputeTerrain.glsl");
        computeMeshShader = new ComputeShader(_gl, AssetPaths.shaderPath + "ComputeMesh.glsl");
        _texture2D = new Texture2D(_gl, paddedTextureAtlas);
    }

    private unsafe void UpdateChunkGpu()
    {
        CreateTexture();
        GenerateChunkGpu(0.0f);
    }
    
    public unsafe void InitializeOpaqueBuffer(bool smartAlloc = false, uint faceCount = 0)
    {
        uint vertexDataSize = 4 * 4 * 2;

        uint size = smartAlloc
            ? faceCount * vertexDataSize * 12 : (vertexDataSize * Chunk.kDefaultChunkSize * Chunk.kDefaultChunkSize * Chunk.kDefaultChunkHeight * 36 / 16);
        
        // Setup VAO
        opaque_vao = _gl.GenVertexArray();
        _gl.BindVertexArray(opaque_vao);
    
        // Create and bind SSBO for opaque voxel vertices
        opaque_ssbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, opaque_ssbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, size, null, BufferUsageARB.DynamicDraw);
        
        // Create buffer for draw commands
        opaque_commandBuffer = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, opaque_commandBuffer);
        _gl.BufferData(BufferTargetARB.DrawIndirectBuffer, (nuint)(sizeof(DrawArraysIndirectCommand)), null, BufferUsageARB.DynamicDraw);
        
        // position + voxelValue (vec)
        _gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexDataSize, 0);
        _gl.EnableVertexAttribArray(0);
        // uv + faceID (vec)
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexDataSize, sizeof(float) * 4);
        _gl.EnableVertexAttribArray(1);
        
        _gl.BindVertexArray(0); // Unbind VAO to prevent accidental modification
    }

    public unsafe void InitializeTransparentBuffer(bool smartAlloc = false, uint faceCount = 0)
    {
        uint vertexDataSize = 4 * 4 * 2;

        uint size = smartAlloc
            ? faceCount * vertexDataSize * 12 : (vertexDataSize * Chunk.kDefaultChunkSize * Chunk.kDefaultChunkSize * Chunk.kDefaultChunkHeight * 36 / 16);
        
        transparent_vao = _gl.GenVertexArray();
        _gl.BindVertexArray(transparent_vao);
        
        // Create and bind SSBO for transparent voxel vertices
        transparent_ssbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, transparent_ssbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, size, null, BufferUsageARB.DynamicDraw);
        
        // Create buffer for draw commands
        transparent_commandBuffer = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, transparent_commandBuffer);
        _gl.BufferData(BufferTargetARB.DrawIndirectBuffer, (nuint)(sizeof(DrawArraysIndirectCommand)), null, BufferUsageARB.DynamicDraw);

        
        // position + voxelValue (vec)
        _gl.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, vertexDataSize, 0);
        _gl.EnableVertexAttribArray(0);
        // uv + faceID (vec)
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexDataSize, sizeof(float) * 4);
        _gl.EnableVertexAttribArray(1);
        
        _gl.BindVertexArray(0); // Unbind VAO to prevent accidental modification
    }

    private unsafe void CreateTexture()
    {
        // Compute Texture
        chunkTextureBuffer = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture3D, chunkTextureBuffer);
        _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
        _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        _gl.TexImage3D(TextureTarget.Texture3D, 0, InternalFormat.R32ui, Chunk.kDefaultChunkSize, Chunk.kDefaultChunkHeight, Chunk.kDefaultChunkSize, 0, PixelFormat.RGInteger, PixelType.UnsignedInt, null);
        _gl.GenerateMipmap(TextureTarget.Texture3D);
        _gl.BindTexture(TextureTarget.Texture3D, 0);
    }

    #region GPU Side Chunk Generation
    
    private void GenerateChunkGpu(float time)
    {
        // Chunk Generation shader and store it to the chunkTexture3D
        computeChunkShader.Use();
            
        computeChunkShader.SetUniform("time", time);
        computeChunkShader.SetUniform("offset", new Vector3(coordinates.X*16, 0, coordinates.Y*16));
        
        // Binding of the chunkData texture3D to 0
        _gl.BindTexture(TextureTarget.Texture3D, chunkTextureBuffer);
        _gl.BindImageTexture (0, chunkTextureBuffer, 0, false, 0, GLEnum.WriteOnly, InternalFormat.R8ui);
        
        // Calculation of the chunk data through a compute shader
        _gl.DispatchCompute(16,16, 16);
        _gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);
        
        // Unbinding of the texture3D
        _gl.BindImageTexture (0, 0, 0, false, 0, GLEnum.WriteOnly, InternalFormat.R8ui);
        _gl.BindTexture(TextureTarget.Texture3D, 0);
            
        computeChunkShader.Unbind();
    }
    
    public unsafe void GenerateMeshGpu(uint faceBlockDataBuffer = 0)
    {
        DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
        {
            Count = 0,
            InstanceCount = 1,
            First = 0,
            BaseInstance = 0
        };
    
        _gl.NamedBufferSubData(opaque_commandBuffer, 0, (nuint)sizeof(DrawArraysIndirectCommand), in cmd);
        
        // Use the compute shader designed to populate the mesh data
        computeMeshShader.Use();
        
        // Adjusts the LOD level
        computeMeshShader.SetUniform("lod", lodLevel);
        
        // Opaque pass
        computeMeshShader.SetUniform("pass", 0);

        // Bind the texture that contains chunk data which will be read by the compute shader
        _gl.BindTexture(TextureTarget.Texture3D, chunkTextureBuffer);
        _gl.BindImageTexture(0, chunkTextureBuffer, 0, false, 0, GLEnum.ReadOnly, InternalFormat.RG32ui);

        // Ensure the Opaque SSBO is bound to the appropriate binding point that the compute shader expects
        _gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 1, opaque_ssbo);

        // Also ensure the command buffer is correctly bound to the shader
        _gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 2, opaque_commandBuffer);
        
        // Bind the faceData texture
        _gl.BindTexture(TextureTarget.Texture1D, faceBlockDataBuffer);
        _gl.BindImageTexture(3, faceBlockDataBuffer, 0, false, 0, GLEnum.ReadOnly, InternalFormat.R8ui);

        // Dispatch compute for opaque mesh
        _gl.DispatchCompute(groups.X / (lodLevel + 1), groups.Y / (lodLevel + 1), groups.Z / (lodLevel + 1));
        
        // Wait for all compute operations to complete before using the data in subsequent rendering calls
        _gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);
        
        computeMeshShader.Use();
        
        // Transparent pass
        computeMeshShader.SetUniform("pass", 1);
        
        // Dispatch compute for transparent mesh
        cmd = new DrawArraysIndirectCommand
        {
            Count = 0,  // Ensure that this count is properly updated in your compute shader or elsewhere necessary
            InstanceCount = 1,
            First = 0,
            BaseInstance = 0
        };
    
        _gl.NamedBufferSubData(transparent_commandBuffer, 0, (nuint)sizeof(DrawArraysIndirectCommand), in cmd);
        
        // Ensure the Transparent SSBO is bound to the appropriate binding point that the compute shader expects
        _gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 1, transparent_ssbo);
        
        // Also ensure the command buffer is correctly bound to the shader
        _gl.BindBufferBase(BufferTargetARB.ShaderStorageBuffer, 2, transparent_commandBuffer);
        
        // Dispatch compute for opaque mesh
        _gl.DispatchCompute(groups.X, groups.Y, groups.Z);
        
        // Wait for all compute operations to complete before using the data in subsequent rendering calls
        _gl.MemoryBarrier(MemoryBarrierMask.AllBarrierBits);

        // Unbind the texture as it is no longer needed
        _gl.BindImageTexture(0, 0, 0, false, 0, GLEnum.ReadOnly, InternalFormat.R8ui);
        _gl.BindImageTexture(3, 0, 0, false, 0, GLEnum.ReadOnly, InternalFormat.R8ui);
        _gl.BindTexture(TextureTarget.Texture3D, 0);
        _gl.BindTexture(TextureTarget.Texture1D, 0);
    }

    public void DeleteChunkData()
    {
        _gl.DeleteTexture(chunkTextureBuffer);
    }
    
    #endregion

    #region CPU Side Chunk Generation
    public unsafe void InitializeChunkDataBuffer(int[,,] data, uint size, uint height)
    {
        uint[] linearData = new uint[size * size * height * 2];
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    int index = (x * ((int) height * (int) size) + y * (int) size + z) * 2;
                    linearData[index] = (uint) (data[x, y, z] == 7 ? 0 : data[x, y, z]);
                    linearData[index + 1] = (uint) (data[x, y, z] == 7 ? data[x, y, z] : 0);
                }
            }
        }

        fixed (void* d = &linearData[0])
        {
            // Texture3D initialisation
            chunkTextureBuffer = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture3D, chunkTextureBuffer);

            _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS,
                (int) TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT,
                (int) TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR,
                (int) TextureWrapMode.ClampToEdge);
            _gl.TexImage3D(TextureTarget.Texture3D, 0, InternalFormat.RG32ui, size, height, size, 0,
                PixelFormat.RGInteger, PixelType.UnsignedInt, d);
            // _gl.GenerateMipmap(TextureTarget.Texture3D);
            _gl.BindTexture(TextureTarget.Texture3D, 0);
        }
    }
    
    #endregion

    public Matrix4x4 Transform(Matrix4x4 model)
    {
        var transform = model * Matrix4x4.CreateTranslation((Get3DCoord() - new Vector3((worldSize.X-1) / 2, 0, (worldSize.Y-1) / 2)) * 16);
        return transform;
    }
    
    public unsafe void DrawOpaque(Texture2D texture)
    {
        texture.Bind(0);
        
        _gl.BindVertexArray(opaque_vao);
        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, opaque_commandBuffer);
        _gl.DrawArraysIndirect(PrimitiveType.Triangles, null);
    }
    
    public unsafe void DrawOpaque()
    {
        DrawOpaque(_texture2D);
    }
    
    public unsafe void DrawTransparent(Texture2D texture)
    {
        texture.Bind(0);
        
        _gl.BindVertexArray(transparent_vao);
        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, transparent_commandBuffer);
        _gl.DrawArraysIndirect(PrimitiveType.Triangles, null);
    }
    
    public unsafe void DrawTransparent()
    {
        DrawTransparent(_texture2D);
    }

    public unsafe void Render(Matrix4x4 view, Matrix4x4 projection, float time)
    {
        // GenerateChunk(time);
        // GenerateMesh();
        
        shader.Use();
        _texture2D.Bind(0);
        
        shader.SetUniform("uView", view);
        shader.SetUniform("uProjection", projection);
        
        var model = Matrix4x4.CreateTranslation((Get3DCoord()/*- new Vector3((worldSize.X-1) / 2, 0, (worldSize.Y-1) / 2)*/));
        shader.SetUniform("uModel", model);
        
        _gl.BindVertexArray(opaque_vao);
        _gl.BindBuffer(BufferTargetARB.DrawIndirectBuffer, opaque_commandBuffer);
        _gl.DrawArraysIndirect(PrimitiveType.Triangles, null);
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
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Libération des ressources managées
                if (shader != null)
                {
                    shader.Dispose();
                }
            }

            // Libération des ressources non managées
            DeleteBuffers();

            _disposed = true;
        }
    }

    private void DeleteBuffers()
    {
        _gl.DeleteBuffer(opaque_ssbo);
        _gl.DeleteBuffer(transparent_ssbo);
        _gl.DeleteBuffer(opaque_commandBuffer);
        _gl.DeleteBuffer(transparent_commandBuffer);
        _gl.DeleteVertexArray(opaque_vao);
        _gl.DeleteVertexArray(transparent_vao);
        _gl.DeleteTexture(chunkTextureBuffer);
    }

    ~ChunkMesh()
    {
        Dispose(false);
    }
}