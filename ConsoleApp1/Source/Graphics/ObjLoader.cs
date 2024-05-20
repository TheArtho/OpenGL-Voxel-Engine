using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using ConsoleApp1.Source.Graphics;
using Minecraft;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

public class ObjLoader
{
    struct Vertex
    {
        Vector3 position;
        Vector3 normal;
        Vector2 texCoords;
    };
    
    private GL _gl;
    private VertexArrayObject<float, uint> _vao;
    private BufferObject<float> _vbo;
    private BufferObject<uint> _ebo;

    private List<Vector3> _vertices = new List<Vector3>();
    private List<Vector2> _uvs = new List<Vector2>();
    private List<Vector3> _normals = new List<Vector3>();
    private List<uint> _vertexIndices = new List<uint>();
    private List<uint> _uvIndices = new List<uint>();
    private List<uint> _normalIndices = new List<uint>();

    private List<float> vertexData = new List<float>();

    private Material material;

    public ObjLoader(GL gl, string path, Material material)
    {
        _gl = gl;
        this.material = material;
        
        Load(path);
        InitializeBuffers();
    }

    public void Load(string path)
    {
        List<uint> vertexIndices = new List<uint>();
        List<uint> uvIndices = new List<uint>();

        try
        {
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("v "))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        _vertices.Add(new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)));
                        
                        // Console.WriteLine($"vertice : {_vertices[^1]}");
                    }
                    else if (line.StartsWith("vt "))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        _uvs.Add(new Vector2(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture)));
                        
                        // Console.WriteLine($"{line}");
                        // Console.WriteLine($"uv : {_uvs[^1]}");
                    }
                    else if (line.StartsWith("vn "))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        _normals.Add(new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)
                            ));
                        
                        // Console.WriteLine($"normal : {_normals[^1]}");
                    }
                    else if (line.StartsWith("f "))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 1; i <= 4; i++)
                        {
                            var vertexParts = parts[i].Split('/');
                            // Vertex
                            _vertexIndices.Add(uint.Parse(vertexParts[0]) - 1);
                            // UV
                            _uvIndices.Add(uint.Parse(vertexParts[1])- 1);
                            // Normals
                            _normalIndices.Add(uint.Parse(vertexParts[2])- 1);
                        }
                    }
                }
                
                Console.WriteLine($"vertexIndice Size : {_vertexIndices.Count}");
                Console.WriteLine($"uvIndices Size : {_uvIndices.Count}");
                Console.WriteLine($"normalIndices Size : {_normalIndices.Count}");
            }
        }
        catch (FormatException e)
        {
            Console.WriteLine($"Erreur de format lors de la lecture du fichier OBJ : {e.Message}");
        }
    }

    private void InitializeBuffers()
    {
        // _normals.ForEach(Console.WriteLine);
        for (int i = 0; i < _vertexIndices.Count - 4; i += 4)
        {
            int vertexIndex, uvIndex, normalIndex;
            
            /// First Triangle Face (0,1,2)
            for (int j = 0; j <= 2; j++)
            {
                vertexIndex = (int)_vertexIndices[i+j];
                uvIndex = (int)_uvIndices[i+j];
                // normalIndex = (int)_indices[i+j];
                
                Console.WriteLine($"{uvIndex}");
                
                vertexData.Add(_vertices[vertexIndex].X);
                vertexData.Add(_vertices[vertexIndex].Y);
                vertexData.Add(_vertices[vertexIndex].Z);
                vertexData.Add(_uvs[uvIndex].X);
                vertexData.Add(_uvs[uvIndex].Y);
            }
            
            // Second Triangle Face (2,3,0)
            for (int j = 2; j <= 3; j++)
            {
                vertexIndex = (int)_vertexIndices[i+j];
                uvIndex = (int)_uvIndices[i+j];
                // normalIndex = (int)_indices[i+j];
                
                Console.WriteLine($"{uvIndex}");
                
                vertexData.Add(_vertices[vertexIndex].X);
                vertexData.Add(_vertices[vertexIndex].Y);
                vertexData.Add(_vertices[vertexIndex].Z);
                vertexData.Add(_uvs[uvIndex].X);
                vertexData.Add(_uvs[uvIndex].Y);
            }
            vertexIndex = (int)_vertexIndices[i];
            uvIndex = (int)_uvIndices[i];
            // normalIndex = (int)_indices[i];
            
            Console.WriteLine($"{uvIndex}");
                
            vertexData.Add(_vertices[vertexIndex].X);
            vertexData.Add(_vertices[vertexIndex].Y);
            vertexData.Add(_vertices[vertexIndex].Z);
            vertexData.Add(_uvs[uvIndex].X);
            vertexData.Add(_uvs[uvIndex].Y);
        }
        
        _vbo = new BufferObject<float>(_gl, new Span<float>(vertexData.ToArray()), BufferTargetARB.ArrayBuffer);
        // _ebo = new BufferObject<uint>(_gl, new Span<uint>(vertexData.ToArray()), BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo);
            
        // Position
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
        // Tex Coord
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
        // Normals
        // _vao.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, 8, 5);
        
        // _vao.unBind();
    }

    public void Render()
    {
        _vao.Bind();
        material.texture.Bind(0);
        
        _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint) vertexData.Count / 5);
        _gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        _vao.Dispose();
        _vbo.Dispose();
    }
}