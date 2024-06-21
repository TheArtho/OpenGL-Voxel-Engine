using System.Numerics;

namespace Minecraft.Game;

public abstract class WorldGenerator
{
    protected int seed;
    
    public WorldGenerator(int seed)
    {
        this.seed = seed;
    }
    
    public abstract void Generate(Chunk chunk);
    public abstract int GetHeight(int x, int y);
}