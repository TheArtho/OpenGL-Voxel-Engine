using Minecraft.Game.Enums;

public class Block
{
    public Blocks UnpackBlockID(int packedData)
    {
        // 24 first bits
        return (Blocks)(packedData & 0xFFFFFF);
    }

    public byte UnpackRotation(int packedData)
    {
        // 24 bits shifts and get 8 next bits
        return (byte)((packedData >> 24) & 0xFF);
    }
    
    public int Pack(Blocks blockID, byte rotation)
    {
        if ((int) blockID > 0xFFFFFF)
        {
            throw new ArgumentException("Block ID exceeds the maximum value that can be packed.");
        }

        // blockID stored in the first 24 bits
        // rotation stored in the next 8 bits
        return (rotation << 24) | (int)blockID;
    }
}