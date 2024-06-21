using Silk.NET.Vulkan;

namespace Minecraft.Game;

public class BlockData
{
    public static List<BlockData> blocks = new List<BlockData>();
    public static uint[] faceBlockData;

    public static BlockData GetBlock(uint blockID)
    {
        if (blockID >= blocks.Count) throw new Exception("Block ID doesn't exist");

        return blocks[(int) blockID];
    }

    public enum Face
    {
        All = -1,
        Front = 0,
        Back = 1,
        Right = 2,
        Left = 3,
        Top = 4,
        Bottom = 5
    }

    public struct FaceData
    {
        public Face faceID;
        public uint textureAtlasID;
    }

    private uint blockID;
    private FaceData[] faces;
    private bool isMultiFace = true;

    public BlockData(uint blockID, JsonData.BlockData data)
    {
        this.blockID = blockID;
        this.isMultiFace = data.IsMultiFace;

        if (isMultiFace)
        {
            faces = new FaceData[6];

            for (int i = 0; i < faces.Length; i++)
            {
                uint[] atlasIDs =
                [
                    ((JsonData.FaceDetails) data.Faces).Front,
                    ((JsonData.FaceDetails) data.Faces).Back,
                    ((JsonData.FaceDetails) data.Faces).Right,
                    ((JsonData.FaceDetails) data.Faces).Left,
                    ((JsonData.FaceDetails) data.Faces).Top,
                    ((JsonData.FaceDetails) data.Faces).Bottom
                ];

                faces[i] = new FaceData()
                {
                    faceID = (Face) i,
                    textureAtlasID = (uint) atlasIDs[i]
                };
            }
        }
        else
        {
            faces = new FaceData[]
            {
                new()
                {
                    faceID = Face.All,
                    textureAtlasID = (uint) data.Face
                }
            };
        }
        
        blocks.Add(this);
    }

    public static void SetFaceBlockDataArray()
    {
        faceBlockData = new uint[64 * 6];

        int index = 0;
        foreach (var b in blocks)
        {
            if (b.isMultiFace)
            {
                for (int i = 0; i < 6; i++)
                {
                    faceBlockData[index + i] = (uint) b.faces[i].textureAtlasID;
                    
                    Console.WriteLine(faceBlockData[index + i]);
                }
            }
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    faceBlockData[index + i] = (uint) b.faces[0].textureAtlasID;
                    
                    Console.WriteLine(faceBlockData[index + i]);
                }
            }

            index += 6;
        }
    }
}