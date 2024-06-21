using Newtonsoft.Json;
using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Minecraft.JsonData;

public class Block
{
    public int BlockID { get; set; }
    public string Reference { get; set; }
}

public class BlockData
{
    public bool IsMultiFace { get; set; }
    public FaceDetails Faces { get; set; }
    
    public int Face { get; set; }
}

public class FaceDetails
{
    public uint Front { get; set; }
    public uint Back { get; set; }
    public uint Right { get; set; }
    public uint Left { get; set; }
    public uint Top { get; set; }
    public uint Bottom { get; set; }

    public override string ToString()
    {
        string str = "";
        str += Front + "\n";
        str += Back + "\n";
        str += Right + "\n";
        str += Left + "\n";
        str += Top + "\n";
        str += Bottom + "\n";
        
        return str;
    }
}