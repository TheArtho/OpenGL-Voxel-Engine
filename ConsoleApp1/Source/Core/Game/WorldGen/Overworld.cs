using System.Numerics;
using Minecraft.Game.Enums;

namespace Minecraft.Game;

public class Overworld : WorldGenerator
{
    private const float baseScale = 0.005f;
    private const float scale = 0.25f;
    
    private const int splineResolution = 50;

    private class HeightData
    {
        public FastNoiseLite noise;
        public FastNoiseLite domainWarp;
        public SplineInterpolator heightSpline;
    }

    private HeightData continentalHeightData;

    private FastNoiseLite VegetationNoise;
    private FastNoiseLite VegetationPlacementNoise;
    
    public Overworld(int seed) : base(seed)
    {
        continentalHeightData = new HeightData
        {
            noise = new FastNoiseLite(),
            domainWarp = new FastNoiseLite(),
            heightSpline = new SplineInterpolator(new Vector2[]
            {
                new Vector2(0, 5),
                new Vector2(0.2f, 5),
                new Vector2(0.3f, 15),
                new Vector2(0.62f, 30),
                new Vector2(0.64f, 50),
                new Vector2(1f, 60),
                /*
                new Vector2(0, 5),
                new Vector2(0.3f, 2),
                new Vector2(0.31f, 20),
                new Vector2(0.38f, 24),
                new Vector2(0.40f, 50),
                new Vector2(0.50f, 55),
                new Vector2(0.75f, (float) 60),
                new Vector2(1, (float) 60),
                */
            })
        };

        continentalHeightData.noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        continentalHeightData.noise.SetFrequency(baseScale * scale);
        continentalHeightData.domainWarp.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
        continentalHeightData.domainWarp.SetDomainWarpAmp(120);
        // continentalHeightData.domainWarp.SetFrequency(0.2f * scale);

        VegetationNoise = new FastNoiseLite();
        VegetationNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        VegetationNoise.SetFrequency(baseScale * scale * 1.3f);
        
        VegetationPlacementNoise = new FastNoiseLite();
        VegetationPlacementNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        VegetationPlacementNoise.SetFrequency(baseScale * scale * 80f);
    }

    public override void Generate(Chunk chunk)
    {
        Vector2 globalPosition = Vector2.One;
        chunk.blockCount = 0;
        
        globalPosition = new Vector2(chunk.PositionX * 16, chunk.PositionZ * 16);

        // Terrain Pass
        
        for (int x = 0; x < chunk.ChunkSize; x++)
        {
            for (int z = 0; z < chunk.ChunkSize; z++)
            {
                globalPosition = new Vector2((float) x * (1 << (int)chunk.lodLevel) + chunk.PositionX * 16, (float) z * (1 << (int)chunk.lodLevel) + chunk.PositionZ * 16);

                int height = GetHeight((int) globalPosition.X * 8, (int) globalPosition.Y * 8);
                
                for (int y = 0; y < chunk.ChunkHeight; y++)
                {
                    int fy = y; // * (1 << (int) chunk.lodLevel);
                    Blocks block = Blocks.Air;

                    if (fy == 0)
                    {
                        block = Blocks.Bedrock;
                    }
                    else if (fy < (height - 5) / (1 << (int) chunk.lodLevel))
                    {
                        block = Blocks.Stone;
                    }
                    else if (fy < height / (1 << (int) chunk.lodLevel))
                    {
                        if (fy > 13 / (1 << (int) chunk.lodLevel))
                        {
                            block = Blocks.Dirt;
                        }
                        else
                        {
                            block = Blocks.Sand;
                        }
                    }   
                    else if (fy == height / (1 << (int) chunk.lodLevel))  // Is surface
                    {
                        if (fy > 13 / (1 << (int) chunk.lodLevel))
                        {
                            block = Blocks.Grass;
                        }
                        else
                        {
                            block = Blocks.Sand;
                        }
                    }
                    
                    if (fy >= height / (1 << (int) chunk.lodLevel))
                    {
                        if (fy <= 13 / (1 << (int) chunk.lodLevel))
                        {
                            block = Blocks.Water;
                        }
                    }

                    if (block != Blocks.Air)
                    {
                        chunk.blockCount++;
                    }

                    chunk.SetBlockAt(x,y,z,(int) block);
                }
            }
        }
        
        // Decoration Pass
        /*
        for (int x = 0; x < Chunk.kDefaultChunkSize; x++)
        {
            for (int z = 0; z < Chunk.kDefaultChunkSize; z++)
            {
                globalPosition = new Vector2((float) x + chunk.PositionX * 16, (float) z + chunk.PositionZ * 16);
                
                int height = GetHeight((int) globalPosition.X * 8, (int) globalPosition.Y * 8);

                double xSlope = double.Abs(GetContinentalnessHeight((int) globalPosition.X * 8 + 16, (int) globalPosition.Y * 8) - GetContinentalnessHeight((int) globalPosition.X * 8 - 16, (int) globalPosition.Y * 8));
                double zSlope = double.Abs(GetContinentalnessHeight((int) globalPosition.X * 8, (int) globalPosition.Y * 8 + 16) - GetContinentalnessHeight((int) globalPosition.X * 8, (int) globalPosition.Y * 8 - 16));
                
                for (int y = 0; y < Chunk.kDefaultChunkHeight; y++)
                {
                    if (y <= 15) continue;
                    
                    if (y == height)
                    {
                        if (xSlope < 1 && zSlope < 1)
                        {
                            if (VegetationNoise.GetNoise((int) globalPosition.X * 8, (int) globalPosition.Y * 8) > 0)
                            {
                                if (VegetationPlacementNoise.GetNoise((int) globalPosition.X * 8, (int) globalPosition.Y * 8) > 0.64f)
                                {
                                    PlaceTree(chunk, x, y, z);
                                }
                            }
                        }
                    }
                }
            }
        }
        */
    }

    public override int GetHeight(int x, int y)
    {
        return (int) GetContinentalnessHeight(x, y);
    }
    
    public int GetDecorationNoise(int x, int y)
    {
        return (int) GetContinentalnessHeight(x, y);
    }

    private double GetContinentalnessHeight(int x, int y)
    {
        float fx = x;
        float fy = y;
        continentalHeightData.domainWarp.DomainWarp(ref fx, ref fy);
        
        float t = (continentalHeightData.noise.GetNoise(fx, fy) + 1) / 2;
        
        // Console.WriteLine(t + " " + continentalHeightData.heightSpline.Interpolate(t));

        return continentalHeightData.heightSpline.Interpolate(t);
    }
    
    public float GetErosion(int x, int y)
    {
        return 0;
    }
    
    public float GetWeirdness(int x, int y)
    {
        return 0;
    }
    
    public float GetTemperature(int x, int y)
    {
        return 0;
    }
    
    public float GetHumidity(int x, int y)
    {
        return 0;
    }

    public uint CalculateHeight(int x, int y, int z)
    {
        return 0;
    }

    private void PlaceTree(Chunk Chunk, int x, int y, int z)
    {
        for (int i = 1; i < 6; i++)
        {
            Blocks block = (Blocks) Chunk.GetBlockAt(x, y + i, z);
            
            if (block != Blocks.Air) continue;

            if (i != 5) // Trunk
            {
                Chunk.SetBlockAt(x, y + i, z, (int) Blocks.Wood);
            }
            else // Leaves
            {
                Chunk.SetBlockAt(x, y + i, z, (int) Blocks.Leaves);

                // for (int ty = 0; ty < tree.Count; ty++)
                // {
                //     for (int tx = 0; tx < tree[ty].Count; tx++)
                //     {
                //         for (int tz = 0; tz < tree[ty][tx].Count; tz++)
                //         {
                //             Chunk.SetBlockAt(x + tx - 2, y + ty + i, z + tz - 2, (int) tree[ty][tx][tz]);
                //         }
                //     }
                // }
            }
        }
    }
}