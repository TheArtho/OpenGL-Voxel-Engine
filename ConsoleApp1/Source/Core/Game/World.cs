using System.Numerics;
using Minecraft.Core;
using Minecraft.Utils;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TextureWrapMode = Silk.NET.OpenGL.TextureWrapMode;

namespace Minecraft.Game;

public class World
{
    public static uint kWorldSize = 40;  // MUST BE EVEN
    
    private GL _gl;

    private ComputeShader computeMeshShader;
    private Texture2D textureAtlas;
    
    private int seed;

    private WorldGenerator generator;
    
    private uint faceBlockDataBuffer;
    
    public Chunk[,] chunkList;
    public Texture2D TextureAtlas => textureAtlas;

    public bool needChunkUpdate;

    public World(GL gl, int seed = 0)
    {
        this._gl = gl;
        this.seed = seed;
        
        TextureAtlasGenerator atlasGenerator = new TextureAtlasGenerator();
        Image<Rgba32> paddedTextureAtlas = atlasGenerator.GeneratePaddedAtlas(Image.Load<Rgba32>(AssetPaths.texturePath + "texture_atlas.png"));
        
        computeMeshShader = new ComputeShader(_gl, AssetPaths.shaderPath + "ComputeMesh.glsl");
        textureAtlas = new Texture2D(_gl, paddedTextureAtlas);

        generator = new Overworld(seed);

        unsafe
        {
            // Texture1D initialisation
            faceBlockDataBuffer = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture1D, faceBlockDataBuffer);

            _gl.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureWrapT,
                (int) TextureWrapMode.ClampToEdge);
            
            fixed (uint* dataPtr = BlockData.faceBlockData)
            {
                _gl.TexImage1D(TextureTarget.Texture1D, 0, InternalFormat.R8ui, (uint) BlockData.faceBlockData.Length, 0, PixelFormat.RedInteger, PixelType.UnsignedInt, dataPtr);
            }
            
            _gl.BindTexture(TextureTarget.Texture1D, 0);
        }
        
        GenerateWorld();
        Console.WriteLine("World data generation finished.");
    }

    private void GenerateWorld()
    {
        chunkList = new Chunk[kWorldSize, kWorldSize];
        
        for (uint x = 0; x < kWorldSize; x++)
        {
            for (uint z = 0; z < kWorldSize; z++)
            {
                chunkList[x, z] = new Chunk(this, (int) x, (int) z);
                
                chunkList[x, z].SetLOD(Engine.MainCamera.Position);
                
                chunkList[x, z].GenerateChunkData(generator);
                chunkList[x, z].SetupMesh(_gl, computeMeshShader);
                chunkList[x, z].GenerateMesh(faceBlockDataBuffer);
                
                Console.WriteLine($"Chunk Populated with {chunkList[x, z].blockCount} blocks and {chunkList[x, z].faceCount} faces.");
            }
        }
    }

    public void UpdateLODs()
    {
        for (int x = 0; x < chunkList.GetLength(0); x++)
        {
            for (int z = 0; z < chunkList.GetLength(1); z++)
            {
                Chunk.Lod lod = chunkList[x,z].lodLevel;
                chunkList[x,z].SetLOD(Engine.MainCamera.Position);

                if (lod == chunkList[x, z].lodLevel) continue;

                chunkList[x, z].GenerateChunkData(generator);
                chunkList[x, z].SetupMesh(_gl, computeMeshShader);
                chunkList[x, z].GenerateMesh(faceBlockDataBuffer);
            }
        }
    }

    public int GetSeed()
    {
        return seed;
    }

    public void DrawOpaque(Shader shader)
    {
        shader.Use();
        
        for (int x = 0; x < chunkList.GetLength(0); x++)
        {
            for (int z = 0; z < chunkList.GetLength(1); z++)
            {
                var model = Matrix4x4.Identity;
                
                model = Matrix4x4.CreateTranslation(new Vector3(0, -10, 0)) * chunkList[x,z].Transform(model);
                shader.SetUniform("uModel", model);
                chunkList[x,z].DrawOpaque();
            }
        }
    }
    
    public void DrawTransparent(Shader shader)
    {
        shader.Use();
        
        for (int x = 0; x < chunkList.GetLength(0); x++)
        {
            for (int z = 0; z < chunkList.GetLength(1); z++)
            {
                var model = Matrix4x4.Identity;
                
                model = Matrix4x4.CreateTranslation(new Vector3(0, -10, 0) - Vector3.UnitY/8) * chunkList[x,z].Transform(model);
                shader.SetUniform("uModel", model);
                chunkList[x,z].DrawTransparent();
            }
        }
    }

    public void Dispose()
    {
        for (int x = 0; x < chunkList.GetLength(0); x++)
        {
            for (int z = 0; z < chunkList.GetLength(1); z++)
            {
                chunkList[x,z].mesh.Dispose();
            }
        }
    }

    public static bool PositionsEquals(Vector3 position1, Vector3 position2)
    {
        if ((int) position1.X / 16 != (int) position2.X / 16)
        {
            return false;
        }
        
        if ((int) position1.Z / 16 != (int) position2.Z / 16)
        {
            return false;
        }

        return true;
    }
}