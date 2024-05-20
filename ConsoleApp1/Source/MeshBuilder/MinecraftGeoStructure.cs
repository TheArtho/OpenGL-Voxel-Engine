namespace ConsoleApp1.Source.Mesh;

using System.Collections.Generic;
using Newtonsoft.Json;

public class GeometryFile
{
    [JsonProperty("format_version")]
    public string FormatVersion { get; set; }

    [JsonProperty("minecraft:geometry")]
    public List<Geometry> Geometry { get; set; }
}

public class Geometry
{
    [JsonProperty("description")]
    public Description Description { get; set; }

    [JsonProperty("bones")]
    public List<Bone> Bones { get; set; }
}

public class Description
{
    [JsonProperty("identifier")]
    public string Identifier { get; set; }

    [JsonProperty("texture_width")]
    public int TextureWidth { get; set; }

    [JsonProperty("texture_height")]
    public int TextureHeight { get; set; }

    [JsonProperty("visible_bounds_width")]
    public float VisibleBoundsWidth { get; set; }

    [JsonProperty("visible_bounds_height")]
    public float VisibleBoundsHeight { get; set; }

    [JsonProperty("visible_bounds_offset")]
    public List<float> VisibleBoundsOffset { get; set; }
}

public class Bone
{
    [JsonProperty("name")]
    public string Name { get; set; }    // Not null

    [JsonProperty("parent")]
    public string? Parent { get; set; }

    [JsonProperty("pivot")]
    public List<float> Pivot { get; set; }    // Not null

    [JsonProperty("cubes")]
    public List<Cube>? Cubes { get; set; }

    [JsonProperty("rotation")]
    public List<float>? Rotation { get; set; }
}

public class Cube
{
    [JsonProperty("origin")]
    public List<float> Origin { get; set; }

    [JsonProperty("size")]
    public List<float> Size { get; set; }

    [JsonProperty("uv")]
    public List<int> Uv { get; set; }

    [JsonProperty("pivot")]
    public List<float> Pivot { get; set; }

    [JsonProperty("rotation")]
    public List<float> Rotation { get; set; }

    [JsonProperty("inflate")]
    public float Inflate { get; set; }

    [JsonProperty("mirror")]
    public bool Mirror { get; set; }
}

public abstract class Uv { }

public class UvSplit : Uv
{
    public class UvParam
    {
        [JsonProperty("uv")]
        public List<float> Uv;
        
        [JsonProperty("uv_size")]
        public List<float> UvSize;
    }

    [JsonProperty("north")]
    public UvParam North;
    [JsonProperty("east")]
    public UvParam East;
    [JsonProperty("south")]
    public UvParam South;
    [JsonProperty("west")]
    public UvParam West;
    [JsonProperty("up")]
    public UvParam Up;
    [JsonProperty("down")]
    public UvParam Down;
}

public class UvSimple : Uv
{
    [JsonProperty("uv")]
    public List<float>? Uv;
}