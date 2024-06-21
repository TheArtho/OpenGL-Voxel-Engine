using System.Diagnostics;
using System.Numerics;
using Minecraft.Game.Enums;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace Minecraft.Game;

public class Chunk
{
    public static uint kDefaultChunkSize = 16;
    public static uint kDefaultChunkHeight = 64;
    public static uint bufferMaxOffset = 0;

    private bool isChunkLoaded = false;
    private bool isChunkMeshed = false;

    private bool isChunkComplete = false;

    private World world;
    
    private int[,,] blocks;

    public ChunkMesh mesh { get; private set; }

    public uint blockCount;
    public uint faceCount;

    public int PositionX { get; set; }
    public int PositionZ { get; set; }

    public enum Lod
    {
        LOD0 = 0,
        LOD1 = 1,
        LOD2 = 2,
        LOD3 = 3
    }

    public Lod lodLevel { get; private set; }

    public uint ChunkSize => kDefaultChunkSize >> (int) lodLevel;
    public uint ChunkHeight => kDefaultChunkHeight >> (int) lodLevel;

    public Chunk(World world, int positionX, int positionZ, Lod lodLevel = 0)
    {
        this.world = world;
        this.PositionX = positionX;
        this.PositionZ = positionZ;
        this.lodLevel = lodLevel;
        
        // Console.WriteLine($"New chunk created at <{PositionX},{PositionZ}>");
    }

    public void SetLOD(Vector3 playerPosition, int maxDistance = 96)
    {
        // Définir les distances pour chaque niveau de LOD
        int[] lodDistances = new[] { 12, 24, 48, maxDistance };
        Lod[] lods = new[] { Lod.LOD0, Lod.LOD1, Lod.LOD2, Lod.LOD3 };

        // Calculer la distance entre le chunk et le joueur
        float distance = Vector3.Distance(new Vector3(PositionX*16, 0, PositionZ*16), playerPosition);

        // Sélectionner le niveau de LOD approprié en fonction de la distance
        int selectedLod = 0; // Par défaut, le plus bas niveau de détail
        for (int i = 0; i < lodDistances.Length; i++)
        {
            if (distance < lodDistances[i] * 16)
            {
                lodLevel = lods[i];
                return;
            }
        }

        lodLevel = Lod.LOD3;
    }
    public void SetupMesh(GL gl, ComputeShader computeMeshShader)
    {
        mesh = new ChunkMesh(
            gl, 
            new Vector2(PositionX, PositionZ), 
            computeMeshShader,
            new Vector3D<uint>((uint) Chunk.kDefaultChunkSize/4, (uint)Chunk.kDefaultChunkHeight/4, (uint)Chunk.kDefaultChunkSize/4)
            );

        mesh.lodLevel = (uint) lodLevel;

        mesh.InitializeOpaqueBuffer(true, faceCount);
        mesh.InitializeTransparentBuffer(true, faceCount);
        // mesh.InitializeTransparentBuffer(true, faceCount);
        /*
        if (blockCount < Chunk.kDefaultChunkSize * Chunk.kDefaultChunkSize * Chunk.kDefaultChunkHeight / 2 - bufferMaxOffset)
        {
             
        }
        else
        {
             mesh.InitializeBuffer();
        }
        */
        //mesh.InitializeBuffer();
    }
    
    /// <summary>
    /// Gets block based on Position
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public int GetBlockAt(int x, int y, int z)
    {
        return blocks[x % ChunkSize, y % ChunkHeight,z % ChunkSize];
    }
    
    /// <summary>
    /// Sets block at local position in the chunk
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="newBlock"></param>
    /// <returns></returns>
    public void SetBlockAt(int x, int y, int z, int newBlock)
    {
        // if (x < PositionX || x >= PositionX + kDefaultChunkSize)
        // {
        //     return;
        // }
        //
        // if (y < 0 || y >= kDefaultChunkHeight)
        // {
        //     return;
        // }
        //
        // if (z < PositionZ || z >= PositionZ + kDefaultChunkSize)
        // {
        //     return;
        // }
        
        blocks[x % ChunkSize, y % ChunkHeight, z % ChunkSize] = newBlock;
    }

    /// <summary>
    /// Get world position of the chunk
    /// </summary>
    public Vector2D<int> GetPosition()
    {
        return new Vector2D<int>(PositionX, PositionZ);
    }
    
    /// <summary>
    /// Get world position of a block in the chunk
    /// </summary>
    public Vector3D<int> WorldToLocalPosition(int x, int y, int z)
    {
        return new Vector3D<int>(PositionX + x, y, PositionZ + z);
    }
    
    /// <summary>
    /// Get world position of a block in the chunk
    /// </summary>
    public Vector3D<int> WorldToLocalPosition(Vector3D<int> localPosition)
    {
        return WorldToLocalPosition(localPosition.X, localPosition.Y, localPosition.Z);
    }

    /// <summary>
    /// Populates the chunk data
    /// </summary>
    public void GenerateChunkData(WorldGenerator generator)
    {
        blocks = new int[ChunkSize, ChunkHeight, ChunkSize];
        
        generator.Generate(this);

        //if (blockCount < Chunk.kDefaultChunkSize * Chunk.kDefaultChunkSize * Chunk.kDefaultChunkHeight / 2 - bufferMaxOffset)
        //{
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    for (int y = 0; y < ChunkHeight; y++)
                    {
                        if (blocks[x, y, z] == (int) Blocks.Air) continue;
                        
                        // Console.WriteLine(6 - CountNeightbors(x, y, z));
                        
                        faceCount += 6 - CountNeightbors(x, y, z);
                    }
                }
            }
        //}
        
        isChunkLoaded = true;
    }

    public uint CountNeightbors(int x, int y, int z)
    {
        uint n = 0;
        
        // Right
        if (x < ChunkSize-1 && blocks[x + 1,y,z] != (int) Blocks.Air)
        {
            n++;
        }
        // Left
        if (x > 0 && blocks[x - 1,y,z] != (int) Blocks.Air)
        {
            n++;
        }
        // Front
        if (z < ChunkSize-1 && blocks[x,y,z + 1] != (int) Blocks.Air)
        {
            n++;
        }
        // Back
        if (z > 0 && blocks[x,y,z - 1] != (int) Blocks.Air)
        {
            n++;
        }
        // Top
        if (y < ChunkHeight-1 && blocks[x,y + 1,z] != (int) Blocks.Air)
        {
            n++;
        }
        // Bottom
        if (y > 0 && blocks[x,y - 1,z] != (int) Blocks.Air)
        {
            n++;
        }

        return n;
    }

    public void StoreChunkToGpu()
    {
        mesh.InitializeChunkDataBuffer(blocks, kDefaultChunkSize, kDefaultChunkHeight);
    }

    /// <summary>
    /// Generates the chunk mesh
    /// </summary>
    public void GenerateMesh(uint faceBlockDataBuffer)
    {
        if (isChunkLoaded)
        {
            mesh.InitializeChunkDataBuffer(blocks, ChunkSize, ChunkHeight);
            mesh.GenerateMeshGpu(faceBlockDataBuffer);
            // mesh.DeleteChunkData();

            isChunkMeshed = true;

            if (lodLevel == Lod.LOD0) isChunkComplete = true;   // TODO move this to another method
        }
        else
        {
            Console.WriteLine($"Can't generate mesh because Chunk {ToString()} isn't loaded.");
        }
    }

    public Matrix4x4 Transform(Matrix4x4 model)
    {
        if (!isChunkMeshed) return Matrix4x4.Identity;
        
        return mesh.Transform(model);
    }
    
    public void DrawOpaque()
    {
        if (isChunkMeshed)
        {
            mesh.DrawOpaque(world.TextureAtlas);
        }
        else
        {
            Console.WriteLine($"Can't draw mesh because Chunk {ToString()} isn't loaded or meshed.");
        }
    }
    
    public void DrawTransparent()
    {
        if (isChunkMeshed)
        {
            mesh.DrawTransparent(world.TextureAtlas);
        }
        else
        {
            Console.WriteLine($"Can't draw mesh because Chunk {ToString()} isn't loaded or meshed.");
        }
    }

    public void Dispose()
    {
        if (mesh != null)
        {
            mesh.Dispose();
        }
    }

    public override string ToString()
    {
        return $"<{PositionX} {PositionZ}>";
    }
}