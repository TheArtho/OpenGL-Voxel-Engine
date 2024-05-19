namespace ConsoleApp1.Source.Mesh;

using Newtonsoft.Json;
using System.IO;

public class JsonMeshLoader
{
    public static GeometryFile LoadGeometryFile(string path)
    {
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<GeometryFile>(json);
    }
}
